using Google.OrTools.ConstraintSolver;
using CVrpPdTwDynamic.Enums;
using CVrpPdTwDynamic.Services;

namespace CVrpPdTwDynamic.Models
{

    public class Routing
    {
        public static RoutingModel CreateRoutingModel(
                                           RoutingIndexManager manager,
                                           DataModel data,
                                           IMapRouter mapRouter
        )
        {

            var customRouter = new CustomRouter(mapRouter);
            RoutingModel routing = new RoutingModel(manager);

            foreach (var op in data.LogisticOperators)
            {
                var callback = routing.RegisterTransitCallback((long fromIndex, long toIndex) =>
                {
                    var fromNode = data.Nodes[manager.IndexToNode(fromIndex)];
                    var toNode = data.Nodes[manager.IndexToNode(toIndex)];
                    return customRouter.GetDistance(op, fromNode, toNode);
                });
                routing.SetArcCostEvaluatorOfVehicle(callback, data.riderMap[op.guid]);
            }

            int demandCallbackIndex = routing.RegisterUnaryTransitCallback((long index) =>
            {
                return data.Nodes[manager.IndexToNode(index)].Demand;
            });

            var vehicleCapacities = data.LogisticOperators.Select(op => op.Capacity).ToArray();
            routing.AddDimensionWithVehicleCapacity(
                demandCallbackIndex,
                0,
                vehicleCapacities, // vehicle maximum capacities
                false,// start cumul to zero
                "Capacity"
            );

            var capacityDimension = routing.GetDimensionOrDie("Capacity");
            foreach (var op in data.LogisticOperators)
            {
                op.StartNode[capacityDimension].SetValue(op.Cargo);
            }

            var timeCallbackIndexAll = data.LogisticOperators
                .Select(op => routing.RegisterTransitCallback((long fromIndex, long toIndex) =>
                    {
                        var fromNode = data.Nodes[manager.IndexToNode(fromIndex)];
                        var toNode = data.Nodes[manager.IndexToNode(toIndex)];
                        return customRouter.GetDuration(op, fromNode, toNode);
                    }))
                .ToArray();

            routing.AddDimensionWithVehicleTransitAndCapacity(
                timeCallbackIndexAll,
                3000,   // no slack
                data.MaxDimension.ToArray(),  // vehicle maximum travel time
                false,  // start cumul to zero
                "Time"
            );

            RoutingDimension timeDimension = routing.GetMutableDimension("Time");

            foreach (var node in data.Nodes)
            {
                node.SetRangeConstraints(timeDimension);
            }

            Solver solver = routing.solver();
            foreach (var order in data.OrdersAndForced)
            {
                if (order.Delivery.guidRider is not null)
                    order.Delivery.Vehicle(routing).SetValue(data.riderMap[order.Delivery.guidRider]);

                if (order.Type != StopType.DeliveryOnly)
                {
                    routing.AddPickupAndDelivery(order.Pickup.Index, order.Delivery.Index);
                    solver.Add(order.Pickup.Vehicle(routing) == order.Delivery.Vehicle(routing));
                    solver.Add(order.Pickup[timeDimension] <= order.Delivery[timeDimension]);
                    routing.AddVariableMinimizedByFinalizer(order.Delivery[timeDimension]);
                }
            }
            return routing;
        }
    }
}