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
                    var fromNode = data.nodeMap.Reverse[manager.IndexToNode(fromIndex)];
                    var toNode = data.nodeMap.Reverse[manager.IndexToNode(toIndex)];
                    return customRouter.GetDistance(op, fromNode, toNode);
                });
                routing.SetArcCostEvaluatorOfVehicle(callback, data.vehicleMap[op.guid]);
            }

            // Capacity Constraints.
            int demandCallbackIndex = routing.RegisterUnaryTransitCallback((long index) =>
            {
                return data.nodeMap.Reverse[manager.IndexToNode(index)].Demand;
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
                var index = manager.NodeToIndex(data.riderMap[op.guid]);
                capacityDimension.CumulVar(index).SetValue(op.Cargo);
            }

            var timeCallbackIndexAll = data.LogisticOperators
                .Select(op => routing.RegisterTransitCallback((long fromIndex, long toIndex) =>
                    {
                        var fromNode = data.nodeMap.Reverse[manager.IndexToNode(fromIndex)];
                        var toNode = data.nodeMap.Reverse[manager.IndexToNode(toIndex)];
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

            foreach (var (node, i) in data.nodeMap.Forward)
            {

                if (node is Start)
                {
                    long index = manager.NodeToIndex(i);
                    timeDimension.CumulVar(index).SetRange(node.StopAfter, node.StopBefore);
                }
                else if (node is Idle end)
                {
                    long index = manager.GetEndIndex(data.vehicleMap[end.guidRider]);
                    timeDimension.CumulVar(index).SetRange(node.StopAfter, node.StopBefore);
                    timeDimension.SetCumulVarSoftUpperBound(index, node.StopAfter, node.DelayPenalty);
                }
                else
                {
                    long index = manager.NodeToIndex(i);
                    timeDimension.CumulVar(index).SetRange(node.StopAfter, node.StopBefore);
                    timeDimension.SetCumulVarSoftUpperBound(index, node.StopAfter, node.DelayPenalty);
                }
            }

            Solver solver = routing.solver();
            foreach (var order in data.OrdersAndForced)
            {
                long deliveryIndex = manager.NodeToIndex(data.nodeMap.Forward[order.ShippingInfo]);
                if (order.ShippingInfo.guidRider is not null)
                    routing.VehicleVar(deliveryIndex).SetValue(data.riderMap[order.ShippingInfo.guidRider]);

                if (order.ShippingInfo.Type != StopType.ForcedStop) // if Shop is not null
                {
                    long pickupIndex = manager.NodeToIndex(data.nodeMap.Forward[order.Shop]);
                    routing.AddPickupAndDelivery(pickupIndex, deliveryIndex);
                    solver.Add(solver.MakeEquality(routing.VehicleVar(pickupIndex), routing.VehicleVar(deliveryIndex)));
                    solver.Add(solver.MakeLessOrEqual(timeDimension.CumulVar(pickupIndex),
                                                      timeDimension.CumulVar(deliveryIndex)));
                    routing.AddVariableMinimizedByFinalizer(timeDimension.CumulVar(deliveryIndex));
                }
            }
            return routing;
        }
    }
}