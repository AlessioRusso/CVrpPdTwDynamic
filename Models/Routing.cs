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


        public long GetDistance(Rider op, INodeInfo fromNode, INodeInfo toNode)
        {
            if (fromNode is StartInfo && toNode is IdleInfo)
                return 0;

            // from pickup to somewhere else
            if (fromNode is Shop && fromNode.guid != toNode.guid)
                return op.PickupFixedFee + _mapRouter.GetDistance(op, fromNode, toNode);

            // from pickup to same pickup
            if (fromNode is Shop && fromNode.guid == toNode.guid)
                return _mapRouter.GetDistance(op, fromNode, toNode);


            // from delivery to somewhere else
            if (fromNode is ShippingInfo)
            {
                if (toNode is IdleInfo)
                    return ((long)op.DeliveryFixedFee * op.Vehicle);
                return ((long)op.DeliveryFixedFee * op.Vehicle) + _mapRouter.GetDistance(op, fromNode, toNode);
            }

            // from rider node that is constrained
            if (fromNode is StartInfo && op.forcedNode is not null)
                if (op.forcedNode.Equals(toNode.guid) == false)
                    return DataModel.Infinite;

            return _mapRouter.GetDistance(op, fromNode, toNode);
        }

        public long GetDuration(Rider op, INodeInfo fromNode, INodeInfo toNode)
        {

            if (fromNode is StartInfo && toNode is IdleInfo)
                return 0;

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
            {
                if (toNode is IdleInfo)
                    return fromShippingInfo.ServiceTime;
                return fromShippingInfo.ServiceTime + _mapRouter.GetDuration(op, fromNode, toNode);
            }

            return _mapRouter.GetDistance(op, fromNode, toNode);
        }
    }

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

            foreach (var (op, index) in data.LogisticOperators.Select((value, index) => (value, index)))
            {
                var callback = routing.RegisterTransitCallback((long fromIndex, long toIndex) =>
                {
                    var fromNode = data.nodeMap.Reverse[manager.IndexToNode(fromIndex)];
                    var toNode = data.nodeMap.Reverse[manager.IndexToNode(toIndex)];
                    return customRouter.GetDistance(op, fromNode, toNode);
                });
                routing.SetArcCostEvaluatorOfVehicle(callback, index);
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
                var index = routing.Start(data.riderMap[op.guid]);
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
                if (node is StartInfo)
                {
                    long index = routing.Start(i);
                    timeDimension.CumulVar(index).SetRange(node.StopAfter, node.StopBefore);
                }
                else if (node is IdleInfo)
                {
                    timeDimension.CumulVar(i).SetRange(node.StopAfter, node.StopBefore);
                    timeDimension.SetCumulVarSoftUpperBound(i, node.StopAfter, node.DelayPenalty);
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

                if (order.Shop is not null)
                {
                    long pickupIndex = manager.NodeToIndex(data.nodeMap.Forward[order.Shop]);
                    routing.AddPickupAndDelivery(pickupIndex, deliveryIndex);
                    solver.Add(solver.MakeEquality(routing.VehicleVar(pickupIndex), routing.VehicleVar(deliveryIndex)));
                    solver.Add(solver.MakeLessOrEqual(timeDimension.CumulVar(pickupIndex),
                                                      timeDimension.CumulVar(deliveryIndex)));
                    routing.AddVariableMinimizedByFinalizer(timeDimension.CumulVar(deliveryIndex));

                    if (order.ShippingInfo.guidRider is not null)
                        routing.VehicleVar(pickupIndex).SetValue(data.riderMap[order.ShippingInfo.guidRider]);
                }
            }
            return routing;
        }
    }
}