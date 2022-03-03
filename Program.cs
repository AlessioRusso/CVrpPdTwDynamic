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

        BiMap<string, int> map = new BiMap<string, int>() { };
        List<Rider> LogisticOperators = new List<Rider>();
        IOReader.ReadRider(riderInput, ref LogisticOperators);

        foreach (var (op, i) in LogisticOperators.Select((value, i) => (value, i)))
        {
            map.Add(op.guid, i);
        }

        List<Tuple<string, string>> Pd = new List<Tuple<string, string>> { };
        BiMap<string, string> Pd_map = new BiMap<string, string>() { };

        List<Order> Orders = new List<Order>();
        IOReader.ReadOrders(standardInput, ref Orders);

        int nodeNumber = LogisticOperators.Count();
        foreach (var order in Orders)
        {
            var pickup = order.Shop.guid;
            var delivery = order.ShippingInfo.guid;
            map.Add(pickup, nodeNumber);
            map.Add(delivery, nodeNumber + 1);
            nodeNumber += 2;
            Pd.Add(Tuple.Create(pickup, delivery));
        }

        foreach (var pair in Pd)
        {
            Pd_map.Add(pair.Item1, pair.Item2);
        }

        DataModel data = DataModel.BuildDataModel(LogisticOperators, Orders, Pd, ref map, 0);

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


        BiMap<string, int> map_new = new BiMap<string, int>() { };
        List<int> present = new List<int> { 1, 1 };

        List<Rider> CurrentLogisticOperators = new List<Rider>();
        State.RiderCurrentState(data, LogisticOperators, ref CurrentLogisticOperators, map, solution_map, present);

        foreach (var (op, i) in CurrentLogisticOperators.Select((value, i) => (value, i)))
        {
            map_new.Add(op.guid, i);
        }

        // locations already visisted
        int past = Solution.VisitedLocations(present);

        int new_loc = 2;
        List<List<Tuple<string, string>>> pd_constraints = new List<List<Tuple<string, string>>>();
        List<Tuple<string, string>> Pd_new = new List<Tuple<string, string>> { };
        List<Order> ForcedDeliveries = new List<Order>();
        List<Order> ForcedPickupDeliviries = new List<Order>();

        List<Order> CurrentOrders = new List<Order>();
        int n_loc = State.LocationsCurrentState(data,
                                                ref CurrentOrders, map,
                                                ref map_new, Pd_map,
                                                solution_map,
                                                present,
                                                ref ForcedDeliveries,
                                                ref ForcedPickupDeliviries,
                                                ref pd_constraints,
                                                ref Pd_new);

        Order newOrder = new Order();
        newOrder.Shop = new Shop();
        newOrder.Shop.Latitude = 10;
        newOrder.Shop.Longitude = 1;
        newOrder.Shop.guid = "nodeD";
        newOrder.Shop.StopAfter = 15;
        newOrder.ProductCount = 0;
        map_new.Add(newOrder.Shop.guid, n_loc + data.vehicleNumber);
        n_loc++;
        newOrder.ShippingInfo = new ShippingInfo();
        newOrder.ShippingInfo.Latitude = 17;
        newOrder.ShippingInfo.Longitude = 1;
        newOrder.ShippingInfo.StopAfter = 30;
        newOrder.ShippingInfo.guid = "nodeDD";
        // newOrder.ShippingInfo.Type = StopType.ForcStop;
        newOrder.ShippingInfo.guidRider = "rider1rider1";

        map_new.Add(newOrder.ShippingInfo.guid, n_loc + data.vehicleNumber);
        n_loc++;
        //Pd_new.Add(Tuple.Create(newOrder.Shop.guid, newOrder.ShippingInfo.guid));
        CurrentOrders.Add(newOrder);
        ForcedPickupDeliviries.Add(newOrder);
        //InputReader.SaveWorldLocations(locations_rider_new, locations_new, map_new, 1);

        data = DataModel.BuildDataModel(CurrentLogisticOperators, CurrentOrders,
                                        Pd_new, ref map_new, past);


        // Create Routing Index Manager
        manager =
            new RoutingIndexManager(data.Locations.GetLength(0), data.vehicleNumber, data.Starts, data.Ends);

        costMatrix = Matrix.ComputeEuclideanCostMatrix(data.Locations);

        costMatrix = Matrix.StartRiderCost(map_new, solution_map, costMatrix, present);

        // Create Routing Model.
        routing = Routing.CreateRoutingModel(manager, data, costMatrix, CurrentLogisticOperators, ForcedDeliveries, ForcedPickupDeliviries, map_new);
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
