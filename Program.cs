using Google.OrTools.ConstraintSolver;
using Google.Protobuf.WellKnownTypes;
using BidirectionalMap;


public class VrpPickupDelivery
{
    class DataModel
    {
        public int VehicleNumber { get; private set; }
        public long[] VehicleCapacities { get; private set; }
        public long[] Cargo { get; private set; }
        public long[] Demands { get; private set; }
        public int[] VehicleCost { get; private set; }
        public int[] VehicleSpeed { get; private set; }
        public long[,] Locations { get; private set; }
        public long[,] TimeWindows { get; private set; }
        public int[][] PickupsDeliveries { get; private set; }
        public int Depot = 0;
        public int pick_service_time = 2;
        public int delivery_service_time = 3;
        public int[] Starts = { };
        public int[] Ends = { };

        public DataModel(int n, List<long> cap, List<long> dem, long[,] loc,
                        long[,] tw, int[][] pickDel, List<int> starts, List<int> ends,
                        List<int> speed, List<int> cost, long[] cargo)
        {
            VehicleNumber = n;
            Starts = starts.ToArray();
            Ends = ends.ToArray();
            VehicleCapacities = cap.ToArray();
            Demands = dem.ToArray();
            Locations = loc;
            TimeWindows = tw;
            PickupsDeliveries = pickDel;
            VehicleCost = new int[this.VehicleNumber];
            VehicleSpeed = new int[this.VehicleNumber];
            for (int i = 0; i < this.VehicleNumber; i++)
            {
                VehicleCost[i] = cost[i];
                VehicleSpeed[i] = speed[i];
            }
            Cargo = cargo;
        }
    };

    static long[,] ComputeEuclideanDistanceMatrix(in long[,] locations)
    {
        // Calculate the distance matrix using Euclidean distance.
        int locationNumber = locations.GetLength(0);
        long[,] distanceMatrix = new long[locationNumber, locationNumber];
        for (int fromNode = 0; fromNode < locationNumber; fromNode++)
        {
            for (int toNode = 0; toNode < locationNumber; toNode++)
            {
                if (fromNode == toNode)
                    distanceMatrix[fromNode, toNode] = 0;
                else
                    distanceMatrix[fromNode, toNode] =
                        (long)Math.Sqrt(Math.Pow(locations[toNode, 0] - locations[fromNode, 0], 2) +
                                        Math.Pow(locations[toNode, 1] - locations[fromNode, 1], 2));
            }
        }
        return distanceMatrix;
    }



    static List<List<Tuple<string, long, long>>> PrintSolution(in DataModel data, in RoutingModel routing, in RoutingIndexManager manager,
                            in Assignment solution, in BiMap<string, int> map)
    {
        List<List<Tuple<string, long, long>>> solution_map = new List<List<Tuple<string, long, long>>>() { };
        Console.WriteLine($"Objective {solution.ObjectiveValue()}:");
        StreamWriter sw = new StreamWriter("solution.csv");
        RoutingDimension timeDimension = routing.GetMutableDimension("Time");
        long totalTime = 0;
        for (int i = 0; i < data.VehicleNumber; ++i)
        {
            List<Tuple<string, long, long>> solution_map_rider = new List<Tuple<string, long, long>>() { };
            Console.WriteLine();
            var index = routing.Start(i);
            while (routing.IsEnd(index) == false)
            {
                var timeVar = timeDimension.CumulVar(index);
                var node = map.Reverse[manager.IndexToNode(index)];
                solution_map_rider.Add(Tuple.Create(node, solution.Min(timeVar), solution.Max(timeVar)));
                Console.Write($"{node} Time ({solution.Min(timeVar)},{solution.Max(timeVar)}) -> ");
                sw.Write(node + " ");
                sw.Write(solution.Min(timeVar).ToString() + " ");
                sw.Write(solution.Max(timeVar).ToString() + " ");
                index = solution.Value(routing.NextVar(index));
            }
            var endTimeVar = timeDimension.CumulVar(index);

            solution_map_rider.Add(Tuple.Create(map.Reverse[manager.IndexToNode(index)], solution.Min(endTimeVar), solution.Max(endTimeVar)));

            Console.WriteLine($"{map.Reverse[manager.IndexToNode(index)]} Time({solution.Min(endTimeVar)},{ solution.Max(endTimeVar)})");
            Console.WriteLine("Time of the route: {0}", solution.Min(endTimeVar));
            totalTime += solution.Min(endTimeVar);
            sw.Write(map.Reverse[manager.IndexToNode(index)]);
            sw.WriteLine();
            solution_map.Add(solution_map_rider);
        }
        Console.WriteLine("");
        Console.WriteLine("Total time of all routes: {0}", totalTime);

        sw.Close();
        return solution_map;
    }



    static string[] SplitInput(StreamReader input)
    {
        string line = input.ReadLine();
        if (line == null) return new string[0];
        return line.Split(new[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
    }


    // The input files follow the "Li & Lim" format
    static void ReadStandardInput(string fileName, ref int n, ref List<long> demands,
                          ref long[,] Locations, ref long[,] tw, ref List<Tuple<string, string>> Pd,
                          ref BiMap<string, int> map)
    {


        using (StreamReader input = new StreamReader(fileName))
        {
            List<Tuple<int, int>> coordinates = new List<Tuple<int, int>>();
            List<Tuple<int, int>> times = new List<Tuple<int, int>>();

            StreamWriter sw = new StreamWriter("locations.csv");

            // read first row
            string[] splitted;
            splitted = SplitInput(input);
            n = int.Parse(splitted[0]);

            // add riders to map 
            for (int i = 0; i < n; i++)
            {
                map.Add($"rider{i + 1}", i + 1);
            }

            // read depot information
            splitted = SplitInput(input);
            map.Add(splitted[0], 0);

            int depotX = int.Parse(splitted[1]);
            int depotY = int.Parse(splitted[2]);
            sw.Write(depotX.ToString() + " ");
            sw.Write(depotY.ToString() + " ");
            sw.WriteLine();

            int ready_depot = int.Parse(splitted[4]);
            int due_depot = int.Parse(splitted[5]);
            demands.Add(0);

            // start reading locations
            int n_loc = 1;
            while (!input.EndOfStream)
            {
                splitted = SplitInput(input);

                if (splitted.Length < 9) break;
                map.Add(splitted[0], n + n_loc);
                n_loc++;
                coordinates.Add(new Tuple<int, int>(int.Parse(splitted[1]), int.Parse(splitted[2])));
                sw.Write(splitted[1].ToString() + " ");
                sw.Write(splitted[2].ToString() + " ");
                sw.WriteLine();
                demands.Add(int.Parse(splitted[3]));
                times.Add(new Tuple<int, int>(int.Parse(splitted[4]), int.Parse(splitted[5])));
                if (splitted[7] == "-")
                {
                    Pd.Add(new Tuple<string, string>(splitted[0], splitted[8]));
                }
            }
            sw.Close();

            // initialize time windows and locations
            int n_nodes = coordinates.Count;
            Locations = new long[n_nodes + 1, 2];
            tw = new long[n_nodes + 1, 2];
            Locations[0, 0] = depotX;
            Locations[0, 1] = depotY;
            tw[0, 0] = ready_depot;
            tw[0, 1] = due_depot;
            int j = 0;

            // build locations 
            foreach (var pair in coordinates)
            {
                Locations[j + 1, 0] = pair.Item1;
                Locations[j + 1, 1] = pair.Item2;
                j++;
            }

            // build time windows
            j = 0;
            foreach (var pair in times)
            {
                tw[j + 1, 0] = pair.Item1;
                tw[j + 1, 1] = pair.Item2;
                j++;
            }

        }
    }

    // The input files follow the "Li & Lim" format
    static void ReadRiderInput(string fileName, ref List<long> cap_rider,
                          ref long[,] Locations_riders, ref long[,] tw_rider,
                          ref List<int> speed, ref List<int> cost, int VehicleNumber)
    {
        if (fileName == "")
        {
            return;
        }

        using (StreamReader input = new StreamReader(fileName))
        {
            StreamWriter sw = new StreamWriter("locations_rider.csv");

            string[] splitted;
            Locations_riders = new long[VehicleNumber, 2];
            tw_rider = new long[VehicleNumber, 2];
            int i = 0;

            while (!input.EndOfStream)
            {
                splitted = SplitInput(input);

                if (splitted.Length < 7) break;

                Locations_riders[i, 0] = int.Parse(splitted[1]);
                Locations_riders[i, 1] = int.Parse(splitted[2]);

                sw.Write(splitted[1].ToString() + " ");
                sw.Write(splitted[2].ToString() + " ");
                sw.WriteLine();
                cap_rider.Add(int.Parse(splitted[5]));
                tw_rider[i, 0] = int.Parse(splitted[3]);
                tw_rider[i, 1] = int.Parse(splitted[4]);
                speed.Add(int.Parse(splitted[6]));
                cost.Add(int.Parse(splitted[7]));
                i = i + 1;
            }
            sw.Close();
        }
    }

    static DataModel BuildDataModel(long[,] locations_rider, long[,] locations, long[,] tw_rider,
                               long[,] tw, List<long> demands, List<long> cap_rider,
                               List<Tuple<string, string>> Pd,
                               BiMap<string, int> map,
                               List<int> speed,
                               List<int> cost,
                               long[] cargo,
                               int VehicleNumber
    )

    {
        List<long> new_demands = new List<long>();
        long[,] new_locations = new long[locations_rider.GetLength(0) + locations.GetLength(0), 2];
        long[,] new_tw = new long[tw_rider.GetLength(0) + tw.GetLength(0), 2];


        List<int> starts = new List<int>();
        List<int> ends = new List<int>();

        for (int i = 0; i < VehicleNumber; i++)
        {
            starts.Add(i + 1);
            ends.Add(0);
        }

        // depot 
        new_locations[0, 0] = locations[0, 0];
        new_locations[0, 1] = locations[0, 1];
        new_tw[0, 0] = tw[0, 0];
        new_tw[0, 1] = tw[0, 1];

        for (int i = 0; i < locations_rider.GetLength(0); i++)
        {
            new_locations[i + 1, 0] = locations_rider[i, 0];
            new_locations[i + 1, 1] = locations_rider[i, 1];
            new_tw[i + 1, 0] = tw_rider[i, 0];
            new_tw[i + 1, 1] = tw_rider[i, 1];
        }
        int j = locations_rider.GetLength(0);
        for (int i = 1; i < locations.GetLength(0); i++)
        {
            if (locations[i, 0] != 0 & locations[i, 1] != 0)
            {
                new_locations[i + j, 0] = locations[i, 0];
                new_locations[i + j, 1] = locations[i, 1];
                new_tw[i + j, 0] = tw[i, 0];
                new_tw[i + j, 1] = tw[i, 1];
            }
        }

        // add demand rider
        for (int i = 0; i < VehicleNumber; i++)
        {
            new_demands.Add(0);
        }

        // add demans depot + locations
        foreach (var d in demands)
            new_demands.Add(d);

        int[][] mapped_pd = new int[Pd.Count][];
        int n_pair = 0;
        foreach (var pair in Pd)
        {
            mapped_pd[n_pair] = new int[] { map.Forward[pair.Item1], map.Forward[pair.Item2] };
            n_pair++;
        }

        // First Run
        return new DataModel(VehicleNumber, cap_rider, new_demands, new_locations, new_tw, mapped_pd, starts, ends, speed, cost, cargo);
    }

    static RoutingModel CreateRoutingModel(RoutingIndexManager manager,
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

        routing.AddDimensionWithVehicleCapacity(demandCallbackIndex, 0, // null capacity slack
                                                data.VehicleCapacities, // vehicle maximum capacities
                                                false,                   // start cumul to zero
                                                "Capacity");

        var capacityDimension = routing.GetDimensionOrDie("Capacity");


        for (int i = 0; i < data.VehicleNumber; i++)
        {
            var index = routing.Start(i);
            capacityDimension.CumulVar(index).SetValue(data.Cargo[i]);
        }

        int[] transitCallbackIndexAll = new int[data.VehicleNumber];
        //populate each vehicle's transitcallback
        for (int i = 0; i < data.VehicleNumber; ++i)
        {
            int j = i;
            transitCallbackIndexAll[i] = routing.RegisterTransitCallback(
            (long fromIndex, long toIndex) =>
            {
                // Convert from routing variable Index to time matrix NodeIndex.
                var fromNode = manager.IndexToNode(fromIndex);
                var toNode = manager.IndexToNode(toIndex);

                if (fromNode == data.Starts[j] || distanceMatrix[fromNode, toNode]==0 )
                {
                    return (int)((distanceMatrix[fromNode, toNode]) * data.VehicleCost[j]) / data.VehicleSpeed[j];
                }

                if (toNode == 0)
                {
                    return 0;
                }

                for (int i = 0; i < data.PickupsDeliveries.Count(); i++)
                {
                    if (fromNode == data.PickupsDeliveries[i][0])
                    {
                        //Console.WriteLine("{0}->{1}={2}", fromNode, toNode, (int)((distanceMatrix[fromNode, toNode] * data.VehicleCost[j])/data.VehicleSpeed[j]) + data.pick_service_time);
                        return (int)((distanceMatrix[fromNode, toNode] * data.VehicleCost[j]) / data.VehicleSpeed[j])
                                    + data.pick_service_time;
                    }
                }
                return (int)((distanceMatrix[fromNode, toNode]) * data.VehicleCost[j]) / data.VehicleSpeed[j]
                            + data.delivery_service_time;

            }
            );
        }

        for (int i = 0; i < data.VehicleNumber; ++i)
        {
            routing.SetArcCostEvaluatorOfVehicle(transitCallbackIndexAll[i], i);
        };

        for (int i = 0; i < data.VehicleNumber; ++i)
        {

            routing.AddDimension(transitCallbackIndexAll[i],
                                0,
                                1000000,
                                true,
                                "TimeRoute");

        }

        RoutingDimension time_routeDimension = routing.GetMutableDimension("TimeRoute");
        time_routeDimension.SetGlobalSpanCostCoefficient(0);

        for (int i = 0; i < data.VehicleNumber; ++i)
        {
            routing.AddDimension(transitCallbackIndexAll[i], // transit callback
                        3000,                   // allow waiting time
                        50000,                   // vehicle maximum capacities
                        false,                // start cumul to zero
                        "Time");
        }
        RoutingDimension timeDimension = routing.GetMutableDimension("Time");

        // Add time window constraints for each location except depot.
        int tw_init = data.VehicleNumber + 1;

        for (int i = tw_init; i < data.TimeWindows.GetLength(0); ++i)
        {
            long index = manager.NodeToIndex(i);
            timeDimension.CumulVar(index).SetRange(data.TimeWindows[i, 0], data.TimeWindows[i, 1]);
            timeDimension.SetCumulVarSoftUpperBound(index, data.TimeWindows[i, 0], 1000000000);
        }
        // Add time window constraints for each vehicle start node.
        for (int i = 0; i < data.VehicleNumber; ++i)
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


    static bool isDeliveryIsPastPickUp(string node, List<Tuple<string, long, long>> route,
                                    BiMap<string, string> Pd_map, int present)
    {
        string pick = null;
        try
        {
            pick = Pd_map.Reverse[node];
        }
        catch { }
        if (pick == null) return false;
        for (int i = 0; i < route.Count; i++)
        {
            if (route[i].Item1.Equals(pick) & i >= present)
            {
                return false;
            }
        }

        return true;
    }

    static bool isDeliveryIsFuturePickUp(string node, List<Tuple<string, long, long>> route,
                                BiMap<string, string> Pd_map, int present)
    {
        string pick = null;
        try
        {
            pick = Pd_map.Reverse[node];
        }
        catch { }
        if (pick == null) return false;
        for (int i = 0; i < route.Count; i++)
        {
            if (route[i].Item1.Equals(pick) & i < present)
            {
                return false;
            }
        }

        return true;
    }


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
        int VehicleNumber = 0;
        List<long> demands = new List<long>();
        long[,] locations = { };
        long[,] tw = { };
        List<Tuple<string, string>> Pd = new List<Tuple<string, string>> { };
        BiMap<string, string> Pd_map = new BiMap<string, string>() { };


        ReadStandardInput(standardInput, ref VehicleNumber, ref demands,
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

        ReadRiderInput(riderInput, ref cap_rider, ref locations_rider, ref tw_rider,
                       ref speed, ref cost, VehicleNumber);

        long[] cargo = new long[VehicleNumber];

        // Build data model
        DataModel data = BuildDataModel(locations_rider, locations, tw_rider, tw, demands, cap_rider,
                                        Pd, map, speed, cost, cargo, VehicleNumber);



        // Create Routing Index Manager
        RoutingIndexManager manager =
            new RoutingIndexManager(data.Locations.GetLength(0), data.VehicleNumber, data.Starts, data.Ends);

        long[,] distanceMatrix = ComputeEuclideanDistanceMatrix(data.Locations);

        // Create Routing Model.
        RoutingModel routing = CreateRoutingModel(manager, data, distanceMatrix, null, null, null);
        // Setting first solution heuristic.
        RoutingSearchParameters searchParameters =
            operations_research_constraint_solver.DefaultRoutingSearchParameters();
        searchParameters.TimeLimit = new Duration { Seconds = 100 };
        //searchParameters.LocalSearchMetaheuristic = LocalSearchMetaheuristic.Types.Value.GuidedLocalSearch;
        searchParameters.FirstSolutionStrategy = FirstSolutionStrategy.Types.Value.PathCheapestArc;
        // Solve the problem.
        Assignment solution = routing.SolveWithParameters(searchParameters);
        // Print solution on console.

        List<List<Tuple<string, long, long>>> solution_map = PrintSolution(data, routing, manager, solution, map);
        BiMap<string, int> map_new = new BiMap<string, int>() { };

        List<int> present = new List<int> { 1, 3};

        long[,] locations_rider_new = new long[data.VehicleNumber, 2];
        long[,] tw_rider_new = new long[data.VehicleNumber, 2];

        long[] cargo_new = new long[data.VehicleNumber];

        for (int i = 0; i < solution_map.Count; i++)
        {
            var toIndex = map.Forward[solution_map[i][present[i]].Item1];
            locations_rider_new[i, 0] = data.Locations[toIndex, 0];
            locations_rider_new[i, 1] = data.Locations[toIndex, 1];
            tw_rider_new[i, 0] = solution_map[i][present[i]].Item2;
            tw_rider_new[i, 1] = 1236;
        }


        for (int i = 0; i < solution_map.Count; i++)
        {
            map_new.Add($"rider{i + 1}", i + 1);
            long actualCargo = 0;
            for (int j = 1; j < solution_map[i].Count & j < present[i]; j++)
            {
                string node_pick;
                try
                {
                    node_pick = Pd_map.Forward[solution_map[i][j].Item1];
                }
                catch
                {
                    node_pick = null;
                }

                if (node_pick != null)
                {
                    actualCargo += data.Demands[map.Forward[solution_map[i][j].Item1]];
                }
                else
                {
                    actualCargo -= data.Demands[map.Forward[solution_map[i][j].Item1]];
                }

            }
            cargo_new[i] = actualCargo;
        }

        int past = 0;
        for (int i = 0; i < present.Count; i++)
        {
            if (present[i] != 0)
                past += present[i] - 1;
        }
        List<long> demands_new = new List<long>();
        long[,] locations_new = new long[locations.GetLength(0) - past, 2];
        long[,] tw_new = new long[locations.GetLength(0) - past, 2];
        List<List<string>> started_deliveries = new List<List<string>>();
        List<List<Tuple<string, string>>> pd_constraints = new List<List<Tuple<string, string>>>();
        List<Tuple<string, string>> Pd_new = new List<Tuple<string, string>> { };


        locations_new[0, 0] = data.Locations[0, 0];
        locations_new[0, 1] = data.Locations[0, 1];
        tw_new[0, 0] = data.TimeWindows[0, 0];
        tw_new[0, 1] = data.TimeWindows[0, 1];

        map_new.Add("depot", 0);
        demands_new.Add(0);

        int n_loc = 1;
        for (int i = 0; i < solution_map.Count; i++)
        {
            List<string> single_delivery = new List<string>();
            List<Tuple<string, string>> pick_delivery_constraint = new List<Tuple<string, string>>();

            for (int j = solution_map[i].Count - 1; j >= present[i]; j--)
            {
                var node = solution_map[i][j].Item1;
                if (isDeliveryIsPastPickUp(node, solution_map[i], Pd_map, present[i]))
                {
                    locations_new[n_loc, 0] = data.Locations[map.Forward[node], 0];
                    locations_new[n_loc, 1] = data.Locations[map.Forward[node], 1];
                    tw_new[n_loc, 0] = data.TimeWindows[map.Forward[node], 0];
                    tw_new[n_loc, 1] = data.TimeWindows[map.Forward[node], 1];
                    demands_new.Add(data.Demands[map.Forward[node]]);
                    map_new.Add(node, n_loc + data.VehicleNumber);
                    n_loc++;
                    single_delivery.Add(node);
                }
                else if (isDeliveryIsFuturePickUp(node, solution_map[i], Pd_map, present[i]))
                {
                    var pickup_node = Pd_map.Reverse[node];
                    locations_new[n_loc, 0] = data.Locations[map.Forward[pickup_node], 0];
                    locations_new[n_loc, 1] = data.Locations[map.Forward[pickup_node], 1];
                    tw_new[n_loc, 0] = data.TimeWindows[map.Forward[pickup_node], 0];
                    tw_new[n_loc, 1] = data.TimeWindows[map.Forward[pickup_node], 1];
                    demands_new.Add(data.Demands[map.Forward[pickup_node]]);
                    map_new.Add(pickup_node, n_loc + data.VehicleNumber);
                    n_loc++;
                    locations_new[n_loc, 0] = data.Locations[map.Forward[node], 0];
                    locations_new[n_loc, 1] = data.Locations[map.Forward[node], 1];
                    tw_new[n_loc, 0] = data.TimeWindows[map.Forward[node], 0];
                    tw_new[n_loc, 1] = data.TimeWindows[map.Forward[node], 1];
                    demands_new.Add(data.Demands[map.Forward[node]]);
                    map_new.Add(node, n_loc + data.VehicleNumber);
                    n_loc++;
                    pick_delivery_constraint.Add(Tuple.Create(pickup_node, node));
                    Pd_new.Add(Tuple.Create(pickup_node, node));
                }

            }
            started_deliveries.Add(single_delivery);
            pd_constraints.Add(pick_delivery_constraint);
        }


        data = BuildDataModel(locations_rider_new, locations_new, tw_rider_new, tw_new, demands_new, cap_rider,
                                    Pd_new, map_new, speed, cost, cargo_new, VehicleNumber);

        // Create Routing Index Manager
        manager =
            new RoutingIndexManager(data.Locations.GetLength(0), data.VehicleNumber, data.Starts, data.Ends);

        distanceMatrix = ComputeEuclideanDistanceMatrix(data.Locations);

        // Create Routing Model.
        routing = CreateRoutingModel(manager, data, distanceMatrix, started_deliveries, pd_constraints, map_new);
        // Setting first solution heuristic.
        searchParameters =
            operations_research_constraint_solver.DefaultRoutingSearchParameters();
        searchParameters.TimeLimit = new Duration { Seconds = 100 };
        //searchParameters.LocalSearchMetaheuristic = LocalSearchMetaheuristic.Types.Value.GuidedLocalSearch;
        searchParameters.FirstSolutionStrategy = FirstSolutionStrategy.Types.Value.PathCheapestArc;
        // Solve the problem.
        solution = routing.SolveWithParameters(searchParameters);
        // Print solution on console.

        solution_map = PrintSolution(data, routing, manager, solution, map_new);

    }
}
