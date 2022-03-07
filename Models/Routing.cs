using Google.OrTools.ConstraintSolver;

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
            // from rider node that is constrained
            if (fromNode is Start && op.forcedNextNode is not null)
                if (op.forcedNextNode != toNode.guid)
                    return DataModel.Infinite;

            if (fromNode is Start && toNode is Idle)
                return 0;

            // from pickup to somewhere else
            if (fromNode is Pickup && fromNode.guid != toNode.guid)
                return op.PickupFixedFee + _mapRouter.GetDistance(op, fromNode, toNode);

            // from pickup to same pickup
            if (fromNode is Pickup && fromNode.guid == toNode.guid)
                return _mapRouter.GetDistance(op, fromNode, toNode); // 0?


            // from delivery to somewhere else
            if (fromNode is Delivery)
            {
                if (toNode is Idle)
                    return ((long)op.DeliveryFixedFee * op.Vehicle);
                return ((long)op.DeliveryFixedFee * op.Vehicle) + _mapRouter.GetDistance(op, fromNode, toNode);
            }

            return _mapRouter.GetDistance(op, fromNode, toNode);
        }

        public long GetDuration(Rider op, INodeInfo fromNode, INodeInfo toNode)
        {

            if (fromNode is Start && toNode is Idle)
                return 0;

            if (fromNode is Pickup fromShop)
            {
                if (fromShop.guid == toNode.guid)
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
            if (fromNode is Delivery fromShippingInfo)
            {
                if (toNode is Idle)
                    return fromShippingInfo.ServiceTime;
                return fromShippingInfo.ServiceTime + _mapRouter.GetDuration(op, fromNode, toNode);
            }

            return _mapRouter.GetDuration(op, fromNode, toNode);
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
                    Console.WriteLine($"{i} {index}");
                    timeDimension.CumulVar(index).SetRange(node.StopAfter, node.StopBefore);
                }
                else if (node is Idle end)
                {
                    long index = manager.GetEndIndex(data.vehicleMap[end.guidRider]);
                    Console.WriteLine($"Idle: {i} {index}");
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

                if (order.Shop is not null)
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