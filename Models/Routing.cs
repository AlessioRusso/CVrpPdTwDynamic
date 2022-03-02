using Google.OrTools.ConstraintSolver;
using BidirectionalMap;

namespace CVrpPdTwDynamic.Models
{
    public class Routing
    {

        public static RoutingModel CreateRoutingModel(
                                           RoutingIndexManager manager,
                                           DataModel data,
                                           long[,] costMatrix,
                                           List<Rider> LogisticOperators,
                                           List<List<string>>? started_deliveries,
                                           List<List<Tuple<string, string>>>? pd_constraints,
                                           BiMap<string, int>? map
                                           )
        {

            RoutingModel routing = new RoutingModel(manager);


            // Capacity Constraints.
            int demandCallbackIndex = routing.RegisterUnaryTransitCallback((long fromIndex) =>
            {
                // Convert from routing variable Index to demand NodeIndex.
                var fromNode = manager.IndexToNode(fromIndex);
                return data.Demands[fromNode];
            });

            // Cost.
            int[] costCallbackIndexAll = new int[data.vehicleNumber];
            foreach (var (op, index) in LogisticOperators.Select((value, index) => (value, index)))
            {
                costCallbackIndexAll[index] = routing.RegisterTransitCallback((long fromIndex, long toIndex) =>
                {
                    var fromNode = manager.IndexToNode(fromIndex);
                    var toNode = manager.IndexToNode(toIndex);

                    // to park node
                    if (toNode == manager.IndexToNode(data.Ends[index]))
                    {
                        return (long)op.DeliveryFixedFee * op.Vehicle;
                    }

                    foreach (var order in data.PickupsDeliveries)
                    {
                        // from pickup to somewhere else
                        if (fromNode == order[0] && costMatrix[fromNode, toNode] > 0)
                            return op.PickupFixedFee + costMatrix[fromNode, toNode];

                        // from delivery to somewhere else
                        if (fromNode == order[1] && costMatrix[fromNode, toNode] > 0)
                            return ((long)op.DeliveryFixedFee * op.Vehicle) + costMatrix[fromNode, toNode];

                        // from delivery (past pickup) to somewhere else 
                        for (int z = 0; started_deliveries != null && z < started_deliveries[index].Count; z++)
                        {
                            if (fromNode == map.Forward[started_deliveries[index][z]] && costMatrix[fromNode, toNode] > 0)
                                return ((long)op.DeliveryFixedFee * op.Vehicle) + costMatrix[fromNode, toNode];
                        }
                    }
                    return costMatrix[fromNode, toNode];
                });
            }

            for (int i = 0; i < data.vehicleNumber; ++i)
            {
                routing.SetArcCostEvaluatorOfVehicle(costCallbackIndexAll[i], i);
            };


            routing.AddDimensionWithVehicleCapacity(demandCallbackIndex, 0, // null capacity slack
                                                    data.vehicleCapacities, // vehicle maximum capacities
                                                    false,                   // start cumul to zero
                                                    "Capacity");

            var capacityDimension = routing.GetDimensionOrDie("Capacity");


            for (int i = 0; i < data.vehicleNumber; i++)
            {
                var index = routing.Start(i);
                capacityDimension.CumulVar(index).SetValue(data.Cargo[i]);
            }

            int[] timeCallbackIndexAll = new int[data.vehicleNumber];
            //populate each vehicle's transitcallback
            foreach (var (op, index) in LogisticOperators.Select((value, index) => (value, index)))
            {

                timeCallbackIndexAll[index] = routing.RegisterTransitCallback(
                (long fromIndex, long toIndex) =>
                {

                    // Convert from routing variable Index to time matrix NodeIndex.
                    var fromNode = manager.IndexToNode(fromIndex);
                    var toNode = manager.IndexToNode(toIndex);
                    if (toNode == manager.IndexToNode(data.Ends[index]))
                    {
                        return data.delivery_service_time;
                    }
                    foreach (var order in data.PickupsDeliveries)
                    {
                        // Double pickup in same node
                        if (fromNode == order[0] && costMatrix[fromNode, toNode] == 0)
                        {
                            return (long)((costMatrix[fromNode, toNode] / op.Vehicle) + DataModel.ServiceTimeSinglePickup);
                        }

                        if (fromNode == order[0])
                        {
                            return (long)((costMatrix[fromNode, toNode] / op.Vehicle) + data.pick_service_time);
                        }
                        if (fromNode == order[1])
                        {
                            return (long)((costMatrix[fromNode, toNode] / op.Vehicle)
                                        + data.delivery_service_time);
                        }

                        // from delivery (past pickup) to somewhere else 
                        for (int p = 0; started_deliveries != null && p < started_deliveries[index].Count; p++)
                        {
                            if (fromNode == map.Forward[started_deliveries[index][p]] && costMatrix[fromNode, toNode] > 0)
                                return (long)((costMatrix[fromNode, toNode] / op.Vehicle)
                                            + data.delivery_service_time);
                        }


                    }
                    return (long)((costMatrix[fromNode, toNode]) / op.Vehicle);
                }
                );
            }

            routing.AddDimensionWithVehicleTransitAndCapacity(
                timeCallbackIndexAll,
                3000,   // no slack
                data.MaxDimension.ToArray(),  // vehicle maximum travel time
                false,  // start cumul to zero
                "Time");

            RoutingDimension timeDimension = routing.GetMutableDimension("Time");

            // Add time window constraints for each location except depot.
            int tw_init = data.vehicleNumber;

            for (int i = tw_init; i < data.TimeWindows.GetLength(0) - data.vehicleNumber; ++i)
            {

                long index = manager.NodeToIndex(i);
                timeDimension.CumulVar(index).SetRange(data.TimeWindows[i, 0], data.TimeWindows[i, 1]);
                timeDimension.SetCumulVarSoftUpperBound(index, data.TimeWindows[i, 0], DataModel.Penalty);
            }

            int rider = 0;
            for (int i = data.TimeWindows.GetLength(0) - data.vehicleNumber; i < data.TimeWindows.GetLength(0); ++i)
            {
                timeDimension.CumulVar(i).SetRange(data.endTurns[rider], DataModel.Infinite);
                timeDimension.SetCumulVarSoftUpperBound(i, data.endTurns[rider], DataModel.Infinite);
                rider++;
            }

            // Add time window constraints for each vehicle start node.
            for (int i = 0; i < data.vehicleNumber; ++i)
            {
                long index = routing.Start(i);
                timeDimension.CumulVar(index).SetRange(data.TimeWindows[i, 0], data.TimeWindows[i, 1]);
            }

            Solver solver = routing.solver();
            foreach (var order in data.PickupsDeliveries)
            {
                long pickupIndex = manager.NodeToIndex(order[0]);
                long deliveryIndex = manager.NodeToIndex(order[1]);
                routing.AddPickupAndDelivery(pickupIndex, deliveryIndex);
                solver.Add(solver.MakeEquality(routing.VehicleVar(pickupIndex), routing.VehicleVar(deliveryIndex)));
                solver.Add(solver.MakeLessOrEqual(timeDimension.CumulVar(pickupIndex),
                                                  timeDimension.CumulVar(deliveryIndex)));
                routing.AddVariableMinimizedByFinalizer(timeDimension.CumulVar(deliveryIndex));
            }

            if (started_deliveries != null)
            {
                foreach (var (deliveriesRider, i) in started_deliveries.Select((value, i) => (value, i)))
                {
                    foreach (var delivery in deliveriesRider)
                    {
                        routing.VehicleVar(manager.NodeToIndex(map.Forward[delivery])).SetValue(i);
                    }
                }
            }

            if (pd_constraints != null)
            {
                foreach (var (ordersRider, i) in pd_constraints.Select((value, i) => (value, i)))
                {
                    foreach (var tuple in ordersRider)
                    {
                        routing.VehicleVar(manager.NodeToIndex(map.Forward[tuple.Item1])).SetValue(i);
                    }
                }
            }

            return routing;
        }

    }
}