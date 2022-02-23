using Google.OrTools.ConstraintSolver;
using Google.Protobuf.WellKnownTypes;
using BidirectionalMap;

namespace CVrpPdTwDynamic.Models
{
    public class Routing
    {
        public static RoutingModel CreateRoutingModel(RoutingIndexManager manager,
                                           DataModel data,
                                           long[,] distanceMatrix,
                                           List<List<string>> started_deliveries,
                                           List<List<Tuple<string, string>>> pd_constraints,
                                           BiMap<string, int> map
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

            // Monetary Constraints.
            int monetaryCallbackIndex = routing.RegisterTransitCallback((long fromIndex, long toIndex) =>
            {
                // Convert from routing variable Index to demand NodeIndex.
                var fromNode = manager.IndexToNode(fromIndex);
                var toNode = manager.IndexToNode(toIndex);
                for (int i = 0; i < data.PickupsDeliveries.Count(); i++)
                {
                    if (fromNode == data.PickupsDeliveries[i][0] && distanceMatrix[fromNode, toNode] > 0)
                        return DataModel.CostPickup;
                }
                return 0;
            });


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

            int[] transitCallbackIndexAll = new int[data.vehicleNumber];
            //populate each vehicle's transitcallback
            for (int i = 0; i < data.vehicleNumber; ++i)
            {
                int j = i;

                transitCallbackIndexAll[i] = routing.RegisterTransitCallback(
                (long fromIndex, long toIndex) =>
                {

                    // Convert from routing variable Index to time matrix NodeIndex.
                    var fromNode = manager.IndexToNode(fromIndex);
                    var toNode = manager.IndexToNode(toIndex);
                    if (toNode == 0)
                    {
                        return 0;
                    }
                    for (int z = 0; z < data.PickupsDeliveries.Count(); z++)
                    {
                        // Double pickup in same node
                        if (fromNode == data.PickupsDeliveries[z][0] && distanceMatrix[fromNode, toNode] == 0)
                        {
                            return (long)((distanceMatrix[fromNode, toNode] * data.vehicleCost[j]) / data.vehicleSpeed[j])
                                            + DataModel.ServiceTimeSinglePickup;
                        }

                        if (fromNode == data.PickupsDeliveries[z][0])
                        {
                            return (long)((distanceMatrix[fromNode, toNode] * data.vehicleCost[j]) / data.vehicleSpeed[j])
                                        + data.pick_service_time;
                        }
                        if (fromNode == data.PickupsDeliveries[z][1])
                        {
                            return (long)((distanceMatrix[fromNode, toNode] * data.vehicleCost[j]) / data.vehicleSpeed[j])
                                        + data.delivery_service_time;
                        }
                    }
                    return (long)((distanceMatrix[fromNode, toNode]) * data.vehicleCost[j]) / data.vehicleSpeed[j];

                }
                );
            }

            for (int i = 0; i < data.vehicleNumber; ++i)
            {
                routing.SetArcCostEvaluatorOfVehicle(transitCallbackIndexAll[i], i);
            };

            routing.AddDimensionWithVehicleTransitAndCapacity(
                transitCallbackIndexAll,
                0,   // no slack
                 new long[] { 50000, 50000 },  // vehicle maximum travel time
                true,  // start cumul to zero
                "TimeRoute");


            RoutingDimension time_routeDimension = routing.GetMutableDimension("TimeRoute");
            time_routeDimension.SetGlobalSpanCostCoefficient(100);


            routing.AddDimension(
                    monetaryCallbackIndex,
                    0,   // no slack
                    50000,  // vehicle maximum travel time
                    true,  // start cumul to zero
                    "MonetaryCost"
            );

            RoutingDimension monetaryCostDimension = routing.GetMutableDimension("MonetaryCost");
            monetaryCostDimension.SetGlobalSpanCostCoefficient(100);


            routing.AddDimensionWithVehicleTransitAndCapacity(
                transitCallbackIndexAll,
                3000,   // no slack
                new long[] { 50000, 50000 },  // vehicle maximum travel time
                false,  // start cumul to zero
                "Time");

            RoutingDimension timeDimension = routing.GetMutableDimension("Time");

            // Add time window constraints for each location except depot.
            int tw_init = data.vehicleNumber + 1;

            for (int i = tw_init; i < data.TimeWindows.GetLength(0); ++i)
            {
                long index = manager.NodeToIndex(i);
                timeDimension.CumulVar(index).SetRange(data.TimeWindows[i, 0], data.TimeWindows[i, 1]);
                timeDimension.SetCumulVarSoftUpperBound(index, data.TimeWindows[i, 0], 100);
            }
            // Add time window constraints for each vehicle start node.
            for (int i = 0; i < data.vehicleNumber; ++i)
            {
                long index = routing.Start(i);
                timeDimension.CumulVar(index).SetRange(data.TimeWindows[i + 1, 0], data.TimeWindows[i + 1, 1]);
            }

            Solver solver = routing.solver();
            for (int i = 0; i < data.PickupsDeliveries.GetLength(0); i++)
            {
                long pickupIndex = manager.NodeToIndex(data.PickupsDeliveries[i][0]);
                long deliveryIndex = manager.NodeToIndex(data.PickupsDeliveries[i][1]);
                routing.AddPickupAndDelivery(pickupIndex, deliveryIndex);
                solver.Add(solver.MakeEquality(routing.VehicleVar(pickupIndex), routing.VehicleVar(deliveryIndex)));
                solver.Add(solver.MakeLessOrEqual(timeDimension.CumulVar(pickupIndex),
                                                  timeDimension.CumulVar(deliveryIndex)));
                routing.AddVariableMinimizedByFinalizer(timeDimension.CumulVar(deliveryIndex));
            }



            if (started_deliveries != null)
            {
                for (int i = 0; i < started_deliveries.Count; i++)
                {
                    for (int j = 0; j < started_deliveries[i].Count; j++)
                    {
                        routing.VehicleVar(manager.NodeToIndex(map.Forward[started_deliveries[i][j]])).SetValue(i);
                    }
                }
            }

            if (pd_constraints != null)
            {
                for (int i = 0; i < pd_constraints.Count; i++)
                {
                    foreach (var tuple in pd_constraints[i])
                    {
                        routing.VehicleVar(manager.NodeToIndex(map.Forward[tuple.Item1])).SetValue(i);
                    }
                }
            }

            return routing;
        }


    }
}