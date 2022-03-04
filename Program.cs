using Google.OrTools.ConstraintSolver;
using Google.Protobuf.WellKnownTypes;
using BidirectionalMap;
using CVrpPdTwDynamic.Models;
using CVrpPdTwDynamic.Utils;

public class VrpPickupDelivery
{
    public static void Main(String[] args)
    {

        var LogisticOperators = new List<Rider>();
        IOReader.ReadRider(args[1], ref LogisticOperators);
        var nodeMap = new BiMap<INodeInfo, int>() { };


        var OrdersAndForced = new List<Order>();
        IOReader.ReadOrders(args[0], OrdersAndForced);


        DataModel data = new DataModel(LogisticOperators, OrdersAndForced);
        // Create Routing Index Manager
        RoutingIndexManager manager = new RoutingIndexManager(
                data.nodeMap.Count(),
                data.LogisticOperators.Count(),
                data.Starts.ToArray(),
                data.Ends.ToArray()
            );

        RoutingModel routing = Routing.CreateRoutingModel(manager, data, new MyMapRouter());
        RoutingSearchParameters searchParameters =
            operations_research_constraint_solver.DefaultRoutingSearchParameters();
        searchParameters.TimeLimit = new Duration { Seconds = 100 };
        searchParameters.FirstSolutionStrategy = FirstSolutionStrategy.Types.Value.PathCheapestArc;
        Assignment solution = routing.SolveWithParameters(searchParameters);
        var solution_map = Solution.PrintSolution(data, routing, manager, solution);
        /*

                BiMap<string, int> CurrentNodeMap = new BiMap<string, int>() { };
                List<int> present = new List<int> { 1, 1 };

                List<Rider> CurrentLogisticOperators = new List<Rider>();
                State.RiderCurrentState(data, LogisticOperators, ref CurrentLogisticOperators, nodeMap, solution_map, present);

                foreach (var (op, i) in CurrentLogisticOperators.Select((value, i) => (value, i)))
                {
                    CurrentNodeMap.Add(op.guid, i);
                }

                // locations already visisted
                int past = Solution.VisitedLocations(present);

                List<Tuple<string, string>> CurrentPickupDeliveries = new List<Tuple<string, string>> { };
                List<Order> ForcedDeliveries = new List<Order>();
                List<Order> ForcedPickupDeliviries = new List<Order>();

                List<Order> CurrentOrders = new List<Order>();
                int n_loc = State.LocationsCurrentState(data,
                                                        ref CurrentOrders, nodeMap,
                                                        ref CurrentNodeMap, PickupDeliveriesMap,
                                                        solution_map,
                                                        present,
                                                        ref ForcedDeliveries,
                                                        ref ForcedPickupDeliviries,
                                                        ref CurrentPickupDeliveries);

                Order newOrder = new Order();
                newOrder.Shop = new Shop();
                newOrder.Shop.Latitude = 10;
                newOrder.Shop.Longitude = 1;
                newOrder.Shop.guid = "nodeD";
                newOrder.Shop.StopAfter = 15;
                newOrder.ProductCount = 0;
                CurrentNodeMap.Add(newOrder.Shop.guid, n_loc + data.vehicleNumber);
                n_loc++;
                newOrder.ShippingInfo = new ShippingInfo();
                newOrder.ShippingInfo.Latitude = 17;
                newOrder.ShippingInfo.Longitude = 1;
                newOrder.ShippingInfo.StopAfter = 30;
                newOrder.ShippingInfo.guid = "nodeDD";
                // newOrder.ShippingInfo.Type = StopType.ForcStop;
                newOrder.ShippingInfo.guidRider = "rider2rider2";

                CurrentNodeMap.Add(newOrder.ShippingInfo.guid, n_loc + data.vehicleNumber);
                n_loc++;
                CurrentPickupDeliveries.Add(Tuple.Create(newOrder.Shop.guid, newOrder.ShippingInfo.guid));
                CurrentOrders.Add(newOrder);
                ForcedPickupDeliviries.Add(newOrder);
                //InputReader.SaveWorldLocations(locations_rider_new, locations_new, map_new, 1);

                data = DataModel.BuildDataModel(CurrentLogisticOperators,
                                                CurrentOrders,
                                                CurrentPickupDeliveries,
                                                ref CurrentNodeMap,
                                                past);


                // Create Routing Index Manager
                manager =
                    new RoutingIndexManager(data.Locations.GetLength(0), data.vehicleNumber, data.Starts, data.Ends);

                costMatrix = Matrix.ComputeEuclideanCostMatrix(data.Locations);

                costMatrix = Matrix.StartRiderCost(CurrentNodeMap, solution_map, costMatrix, present);

                // Create Routing Model.
                routing = Routing.CreateRoutingModel(manager, data, costMatrix, CurrentLogisticOperators, ForcedDeliveries, ForcedPickupDeliviries, CurrentNodeMap);
                // Setting first solution heuristic.
                searchParameters =
                    operations_research_constraint_solver.DefaultRoutingSearchParameters();
                searchParameters.TimeLimit = new Duration { Seconds = 100 };
                //searchParameters.LocalSearchMetaheuristic = LocalSearchMetaheuristic.Types.Value.GuidedLocalSearch;
                searchParameters.FirstSolutionStrategy = FirstSolutionStrategy.Types.Value.PathCheapestArc;
                // Solve the problem.
                solution = routing.SolveWithParameters(searchParameters);
                // Print solution on console.
                solution_map = Solution.PrintSolution(data, routing, manager, solution, CurrentNodeMap, 1); */
    }
}
