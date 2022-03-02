using Google.OrTools.ConstraintSolver;
using Google.Protobuf.WellKnownTypes;
using BidirectionalMap;
using CVrpPdTwDynamic.Models;
using CVrpPdTwDynamic.Utils;

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

        List<long> demands = new List<long>();

        long[,] locations = { };
        long[,] tw = { };
        List<Tuple<string, string>> Pd = new List<Tuple<string, string>> { };
        BiMap<string, string> Pd_map = new BiMap<string, string>() { };


        IOReader.ReadStandardInput(standardInput, ref demands, ref locations, ref tw, ref Pd, ref map);

        foreach (var pair in Pd)
        {
            Pd_map.Add(pair.Item1, pair.Item2);
        }

        List<Rider> LogisticOperators = new List<Rider>();
        IOReader.ReadRiderInput(riderInput, ref LogisticOperators);

        foreach (var (op, i) in LogisticOperators.Select((value, i) => (value, i)))
        {
            map.Add(op.guid, i);
        }

        // Build data model
        DataModel data = DataModel.BuildDataModel(LogisticOperators, locations, tw, demands, Pd, ref map);


        // Create Routing Index Manager
        RoutingIndexManager manager =
            new RoutingIndexManager(data.Locations.GetLength(0), data.vehicleNumber, data.Starts, data.Ends);

        long[,] costMatrix = Matrix.ComputeEuclideanCostMatrix(data.Locations);

        RoutingModel routing = Routing.CreateRoutingModel(manager, data, costMatrix, LogisticOperators, null, null, null);
        RoutingSearchParameters searchParameters =
            operations_research_constraint_solver.DefaultRoutingSearchParameters();
        searchParameters.TimeLimit = new Duration { Seconds = 100 };
        searchParameters.FirstSolutionStrategy = FirstSolutionStrategy.Types.Value.PathCheapestArc;
        Assignment solution = routing.SolveWithParameters(searchParameters);
        List<List<Tuple<string, long, long>>> solution_map = Solution.PrintSolution(data, routing, manager, solution, map, 0);


        /*
                BiMap<string, int> map_new = new BiMap<string, int>() { };
                List<int> present = new List<int> { 2, 1, 0 };

                List<Rider> CurrentLogisticOperators = new List<Rider>();
                State.RiderCurrentState(data, LogisticOperators, ref CurrentLogisticOperators, map, solution_map, present);

                foreach (var (op, i) in CurrentLogisticOperators.Select((value, i) => (value, i)))
                {
                    map_new.Add(op.guid, i);
                }

                // locations already visisted
                int past = Solution.VisitedLocations(present);

                int new_loc = 2;
                List<long> demands_new = new List<long>();
                long[,] locations_new = new long[locations.GetLength(0) - past + new_loc, 2];
                long[,] tw_new = new long[locations.GetLength(0) - past + new_loc, 2];
                List<List<string>> started_deliveries = new List<List<string>>();
                List<List<Tuple<string, string>>> pd_constraints = new List<List<Tuple<string, string>>>();
                List<Tuple<string, string>> Pd_new = new List<Tuple<string, string>> { };

                int n_loc = State.LocationsCurrentState(data, ref locations_new, ref tw_new, ref demands_new, map,
                                            ref map_new, Pd_map, solution_map, present, ref started_deliveries,
                                            ref pd_constraints, ref Pd_new);

                locations_new[n_loc, 0] = 10;
                locations_new[n_loc, 1] = 1;
                tw_new[n_loc, 0] = 15;
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

                //InputReader.SaveWorldLocations(locations_rider_new, locations_new, map_new, 1);
                data = DataModel.BuildDataModel(CurrentLogisticOperators, locations_new, tw_new, demands_new,
                                                Pd_new, ref map_new);


                // Create Routing Index Manager
                manager =
                    new RoutingIndexManager(data.Locations.GetLength(0), data.vehicleNumber, data.Starts, data.Ends);

                costMatrix = Matrix.ComputeEuclideanCostMatrix(data.Locations);

                costMatrix = Matrix.StartRiderCost(map_new, solution_map, costMatrix, present);

                // Create Routing Model.
                routing = Routing.CreateRoutingModel(manager, data, costMatrix, CurrentLogisticOperators, started_deliveries, pd_constraints, map_new);
                // Setting first solution heuristic.
                searchParameters =
                    operations_research_constraint_solver.DefaultRoutingSearchParameters();
                searchParameters.TimeLimit = new Duration { Seconds = 100 };
                //searchParameters.LocalSearchMetaheuristic = LocalSearchMetaheuristic.Types.Value.GuidedLocalSearch;
                searchParameters.FirstSolutionStrategy = FirstSolutionStrategy.Types.Value.PathCheapestArc;
                // Solve the problem.
                solution = routing.SolveWithParameters(searchParameters);
                // Print solution on console.
                solution_map = Solution.PrintSolution(data, routing, manager, solution, map_new, 1);*/
    }
}
