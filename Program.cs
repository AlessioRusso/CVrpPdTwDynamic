using Google.OrTools.ConstraintSolver;
using Google.Protobuf.WellKnownTypes;
using BidirectionalMap;
using CVrpPdTwDynamic.Models;
using CVrpPdTwDynamic.Utils;
using CVrpPdTwDynamic.Checker;


public class VrpPickupDelivery
{
    public static void Main(String[] args)
    {
        string standardInput = "";
        string riderInput = "";

        if (args.Length == 2)
        {
            standardInput = args[0];
            riderInput = args[1];
        }
        else
        {
            Console.WriteLine("Invalid args");
            return;
        }

        // Get PickUps and Deliveries informations
        BiMap<string, int> map = new BiMap<string, int>() { };
        int vehicleNumber = 0;
        List<long> demands = new List<long>();

        long[,] locations = { };
        long[,] tw = { };
        List<Tuple<string, string>> Pd = new List<Tuple<string, string>> { };
        BiMap<string, string> Pd_map = new BiMap<string, string>() { };


        IOReader.ReadStandardInput(standardInput, ref vehicleNumber, ref demands,
                          ref locations, ref tw, ref Pd, ref map);

        foreach (var pair in Pd)
        {
            Pd_map.Add(pair.Item1, pair.Item2);
        }

        // Get riders information
        List<long> cap_rider = new List<long>();
        long[,] locations_rider = { };
        long[,] tw_rider = { };
        List<int> speed = new List<int>();
        List<int> cost = new List<int>();
        List<int> endsTurn = new List<int>();


        IOReader.ReadRiderInput(riderInput, ref cap_rider, ref locations_rider, ref tw_rider,
                       ref speed, ref cost, ref endsTurn, vehicleNumber);

        //InputReader.SaveWorldLocations(locations_rider, locations, map, 0);

        long[] cargo = new long[vehicleNumber];

        // Build data model
        DataModel data = DataModel.BuildDataModel(locations_rider, locations, tw_rider, tw, demands, cap_rider,
                                        Pd, ref map, speed, cost, cargo, endsTurn, vehicleNumber);


        // Create Routing Index Manager
        RoutingIndexManager manager =
            new RoutingIndexManager(data.Locations.GetLength(0), data.vehicleNumber, data.Starts, data.Ends);

        long[,] costMatrix = Matrix.ComputeEuclideanCostMatrix(data.Locations);

        RoutingModel routing = Routing.CreateRoutingModel(manager, data, costMatrix, null, null, null);
        RoutingSearchParameters searchParameters =
            operations_research_constraint_solver.DefaultRoutingSearchParameters();
        searchParameters.TimeLimit = new Duration { Seconds = 100 };
        searchParameters.FirstSolutionStrategy = FirstSolutionStrategy.Types.Value.PathCheapestArc;
        Assignment solution = routing.SolveWithParameters(searchParameters);


        List<List<Tuple<string, long, long>>> solution_map = Solution.PrintSolution(data, routing, manager, solution, map, 0);
        BiMap<string, int> map_new = new BiMap<string, int>() { };

        List<int> present = new List<int> { 0, 3 };

        long[,] locations_rider_new = new long[data.vehicleNumber, 2];
        long[,] tw_rider_new = new long[data.vehicleNumber, 2];

        long[] cargo_new = new long[data.vehicleNumber];

        for (int i = 0; i < solution_map.Count; i++)
        {
            var toIndex = map.Forward[solution_map[i][present[i]].Item1];
            // park state
            if (toIndex == data.Ends[i])
            {
                var prevIndex = map.Forward[solution_map[i][present[i] - 1].Item1];
                locations_rider_new[i, 0] = data.Locations[prevIndex, 0];
                locations_rider_new[i, 1] = data.Locations[prevIndex, 1];
                tw_rider_new[i, 0] = solution_map[i][present[i] - 1].Item2 + DataModel.CostDelivery;
                tw_rider_new[i, 1] = solution_map[i][present[i] - 1].Item3 + DataModel.CostDelivery;
            }
            else
            {
                locations_rider_new[i, 0] = data.Locations[toIndex, 0];
                locations_rider_new[i, 1] = data.Locations[toIndex, 1];
                tw_rider_new[i, 0] = solution_map[i][present[i]].Item2;
                tw_rider_new[i, 1] = solution_map[i][present[i]].Item3;
            }
        }


        for (int i = 0; i < solution_map.Count; i++)
        {
            map_new.Add($"rider{i + 1}", i);
            long actualCargo = 0;
            for (int j = 1; j < solution_map[i].Count && j < present[i]; j++)
            {
                actualCargo += data.Demands[map.Forward[solution_map[i][j].Item1]];
            }
            cargo_new[i] = actualCargo;
        }

        int past = 0;
        for (int i = 0; i < present.Count; i++)
        {
            if (present[i] != 0)
                past += present[i] - 1;
        }


        int new_loc = 2;
        List<long> demands_new = new List<long>();
        long[,] locations_new = new long[locations.GetLength(0) - past + new_loc, 2];
        long[,] tw_new = new long[locations.GetLength(0) - past + new_loc, 2];
        List<List<string>> started_deliveries = new List<List<string>>();
        List<List<Tuple<string, string>>> pd_constraints = new List<List<Tuple<string, string>>>();
        List<Tuple<string, string>> Pd_new = new List<Tuple<string, string>> { };




        int n_loc = 0;
        for (int i = 0; i < solution_map.Count; i++)
        {
            List<string> single_delivery = new List<string>();
            List<Tuple<string, string>> pick_delivery_constraint = new List<Tuple<string, string>>();

            for (int j = solution_map[i].Count - 1; j >= present[i]; j--)
            {
                var node = solution_map[i][j].Item1;
                if (CheckOrders.isDeliveryIsPastPickUp(node, solution_map[i], Pd_map, present[i]))
                {
                    locations_new[n_loc, 0] = data.Locations[map.Forward[node], 0];
                    locations_new[n_loc, 1] = data.Locations[map.Forward[node], 1];
                    tw_new[n_loc, 0] = data.TimeWindows[map.Forward[node], 0];
                    tw_new[n_loc, 1] = data.TimeWindows[map.Forward[node], 1];
                    demands_new.Add(data.Demands[map.Forward[node]]);
                    map_new.Add(node, n_loc + data.vehicleNumber);
                    n_loc++;
                    single_delivery.Add(node);
                }
                else if (CheckOrders.isDeliveryIsFuturePickUp(node, solution_map[i], Pd_map, present[i]))
                {
                    var pickup_node = Pd_map.Reverse[node];
                    locations_new[n_loc, 0] = data.Locations[map.Forward[pickup_node], 0];
                    locations_new[n_loc, 1] = data.Locations[map.Forward[pickup_node], 1];
                    tw_new[n_loc, 0] = data.TimeWindows[map.Forward[pickup_node], 0];
                    tw_new[n_loc, 1] = data.TimeWindows[map.Forward[pickup_node], 1];
                    demands_new.Add(data.Demands[map.Forward[pickup_node]]);
                    map_new.Add(pickup_node, n_loc + data.vehicleNumber);
                    n_loc++;
                    locations_new[n_loc, 0] = data.Locations[map.Forward[node], 0];
                    locations_new[n_loc, 1] = data.Locations[map.Forward[node], 1];
                    tw_new[n_loc, 0] = data.TimeWindows[map.Forward[node], 0];
                    tw_new[n_loc, 1] = data.TimeWindows[map.Forward[node], 1];
                    demands_new.Add(data.Demands[map.Forward[node]]);
                    map_new.Add(node, n_loc + data.vehicleNumber);
                    n_loc++;
                    if (CheckOrders.isPresentPickUp(pickup_node, solution_map[i], present[i]))
                    {
                        pick_delivery_constraint.Add(Tuple.Create(pickup_node, node));
                    }
                    Pd_new.Add(Tuple.Create(pickup_node, node));
                }

            }
            started_deliveries.Add(single_delivery);
            pd_constraints.Add(pick_delivery_constraint);
        }


        locations_new[n_loc, 0] = 10;
        locations_new[n_loc, 1] = 1;
        tw_new[n_loc, 0] = 20;
        tw_new[n_loc, 1] = 1000;
        demands_new.Add(10);
        map_new.Add("nodeD", n_loc + data.vehicleNumber);
        n_loc++;
        locations_new[n_loc, 0] = 17;
        locations_new[n_loc, 1] = 1;
        tw_new[n_loc, 0] = 30;
        tw_new[n_loc, 1] = 800;
        demands_new.Add(-10);
        map_new.Add("nodeDD", n_loc + data.vehicleNumber);
        n_loc++;
        Pd_new.Add(Tuple.Create("nodeD", "nodeDD"));
        List<Tuple<string, string>> newOrders = new List<Tuple<string, string>> { };

        newOrders.Add(Tuple.Create("nodeD", "nodeDD"));

        //InputReader.SaveWorldLocations(locations_rider_new, locations_new, map_new, 1);
        data = DataModel.BuildDataModel(locations_rider_new, locations_new, tw_rider_new, tw_new, demands_new, cap_rider,
                                    Pd_new, ref map_new, speed, cost, cargo_new, endsTurn, vehicleNumber);


        // Create Routing Index Manager
        manager =
            new RoutingIndexManager(data.Locations.GetLength(0), data.vehicleNumber, data.Starts, data.Ends);

        costMatrix = Matrix.ComputeEuclideanCostMatrix(data.Locations);

        costMatrix = Matrix.StartRiderCost(map_new, solution_map, costMatrix, present);

        // Create Routing Model.
        routing = Routing.CreateRoutingModel(manager, data, costMatrix, started_deliveries, pd_constraints, map_new);
        // Setting first solution heuristic.
        searchParameters =
            operations_research_constraint_solver.DefaultRoutingSearchParameters();
        searchParameters.TimeLimit = new Duration { Seconds = 100 };
        //searchParameters.LocalSearchMetaheuristic = LocalSearchMetaheuristic.Types.Value.GuidedLocalSearch;
        searchParameters.FirstSolutionStrategy = FirstSolutionStrategy.Types.Value.PathCheapestArc;
        // Solve the problem.
        solution = routing.SolveWithParameters(searchParameters);
        // Print solution on console.
        solution_map = Solution.PrintSolution(data, routing, manager, solution, map_new, 1);
    }
}
