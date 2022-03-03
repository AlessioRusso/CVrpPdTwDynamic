using Google.OrTools.ConstraintSolver;
using BidirectionalMap;

namespace CVrpPdTwDynamic.Models
{
    public class CustomRouter
    {
        private readonly IMapRouter _mapRouter;

        public CustomRouter(IMapRouter mapRouter)
        {
            this._mapRouter = mapRouter;
        }

        public long GetDuration(Rider op, INodeInfo fromNode, INodeInfo toNode)
        {
            if (fromNode is Shop fromShop)
            {
                if (toNode is Shop toShop && fromShop.guid == toShop.guid)
                {
                    // Double pickup in same node
                    return fromShop.SinglePickupServiceTime; // duration is 0
                }
                else
                {
                    // from pickup to somewhere else (pickup or delivery, does not matter)
                    return fromShop.BaseServiceTime + _mapRouter.GetDuration(op, fromNode, toNode);
                }
            }

            // from delivery to somewhere else
            if (fromNode is ShippingInfo fromShippingInfo)
                return fromShippingInfo.ServiceTime + _mapRouter.GetDuration(op, fromNode, toNode);

            return _mapRouter.GetDistance(op, fromNode, toNode);
        }
    }

    public class Routing
    {


        public static RoutingModel CreateRoutingModel(
                                           RoutingIndexManager manager,
                                           DataModel data,
                                           IMapRouter mapRouter,
                                           List<Rider> LogisticOperators,
                                           BiMap<INodeInfo, int> nodeMap,

                                           List<Order> ordersAndForced
        )
        {
            var riderMap = LogisticOperators
                .Select((op, i) => (op.guid, i))
                .ToDictionary(pair => pair.guid, pair => pair.i);

            var router = new CustomRouter(mapRouter);

            RoutingModel routing = new RoutingModel(manager);

            foreach (var (op, index) in LogisticOperators.Select((value, index) => (value, index)))
            {
                var callback = routing.RegisterTransitCallback((long fromIndex, long toIndex) =>
                {
                    var fromNode = nodeMap.Reverse[manager.IndexToNode(fromIndex)];
                    var toNode = nodeMap.Reverse[manager.IndexToNode(toIndex)];

                    // from pickup to somewhere else
                    if (fromNode is Shop && fromNode.guid != toNode.guid)
                        return op.PickupFixedFee + mapRouter.GetDistance(op, fromNode, toNode);

                    // from delivery to somewhere else
                    if (fromNode is ShippingInfo)
                        return ((long)op.DeliveryFixedFee * op.Vehicle) + mapRouter.GetDistance(op, fromNode, toNode);

                    return mapRouter.GetDistance(op, fromNode, toNode);
                });
                routing.SetArcCostEvaluatorOfVehicle(callback, index);
            }

            // Capacity Constraints.
            int demandCallbackIndex = routing.RegisterUnaryTransitCallback((long index) =>
            {
                // Convert from routing variable Index to demand NodeIndex.
                var node = nodeMap.Reverse[manager.IndexToNode(index)];
                return node.Demand;
            });

            var vehicleCapacities = LogisticOperators.Select(op => op.Capacity).ToArray();
            routing.AddDimensionWithVehicleCapacity(demandCallbackIndex, 0, // null capacity slack
                                                    vehicleCapacities, // vehicle maximum capacities
                                                    false,                  // start cumul to zero
                                                    "Capacity");

            var capacityDimension = routing.GetDimensionOrDie("Capacity");

            foreach (var op in LogisticOperators)
            {
                var index = routing.Start(riderMap[op.guid]);
                capacityDimension.CumulVar(index).SetValue(op.Cargo);
            }

            var timeCallbackIndexAll = LogisticOperators
                .Select(op => routing.RegisterTransitCallback((long fromIndex, long toIndex) =>
                    {
                        var fromNode = nodeMap.Reverse[manager.IndexToNode(fromIndex)];
                        var toNode = nodeMap.Reverse[manager.IndexToNode(toIndex)];
                        return router.GetDuration(op, fromNode, toNode);
                    })).ToArray();

            routing.AddDimensionWithVehicleTransitAndCapacity(
                timeCallbackIndexAll,
                3000,   // no slack
                data.MaxDimension.ToArray(),  // vehicle maximum travel time
                false,  // start cumul to zero
                "Time");

            RoutingDimension timeDimension = routing.GetMutableDimension("Time");

            // Add time window constraints for each location except depot.
            foreach (var (node, i) in nodeMap.Forward)
            {
                long index = manager.NodeToIndex(i);
                timeDimension.CumulVar(index).SetRange(node.StopAfter, node.StopBefore);
                if (node.DelayPenalty != 0)
                {
                    timeDimension.SetCumulVarSoftUpperBound(index, node.StopAfter, node.DelayPenalty);
                }
            }

            Solver solver = routing.solver();
            foreach (var order in ordersAndForced)
            {
                long deliveryIndex = manager.NodeToIndex(nodeMap.Forward[order.ShippingInfo]);
                if (order.ShippingInfo.guidRider is not null)
                    routing.VehicleVar(deliveryIndex).SetValue(riderMap[order.ShippingInfo.guidRider]);

                if (order.Shop is not null)
                {
                    long pickupIndex = manager.NodeToIndex(nodeMap.Forward[order.Shop]);
                    routing.AddPickupAndDelivery(pickupIndex, deliveryIndex);
                    solver.Add(solver.MakeEquality(routing.VehicleVar(pickupIndex), routing.VehicleVar(deliveryIndex)));
                    solver.Add(solver.MakeLessOrEqual(timeDimension.CumulVar(pickupIndex),
                                                      timeDimension.CumulVar(deliveryIndex)));
                    routing.AddVariableMinimizedByFinalizer(timeDimension.CumulVar(deliveryIndex));

                    if (order.ShippingInfo.guidRider is not null)
                        routing.VehicleVar(pickupIndex).SetValue(riderMap[order.ShippingInfo.guidRider]);
                }
            }
            return routing;
        }
    }
}