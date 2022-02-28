using BidirectionalMap;
namespace CVrpPdTwDynamic.Models
{
    public class DataModel
    {
        public int vehicleNumber { get; private set; }
        public long[] vehicleCapacities { get; private set; }
        public long[] Cargo { get; private set; }
        public long[] Demands { get; private set; }
        public int[] vehicleCost { get; private set; }
        public int[] vehicleSpeed { get; private set; }
        public long[,] Locations { get; private set; }
        public long[,] TimeWindows { get; private set; }
        public int[][] PickupsDeliveries { get; private set; }

        public const int CostPickup = 4; // cents
        public const int CostDelivery = 1; // cents

        public const int ServiceTimeSinglePickup = 1;
        public const int Infinite = 100000000;
        public const int Penalty = 3;
        public int pick_service_time = 5;
        public int delivery_service_time = 1;
        public int[] Starts = { };
        public int[] Ends = { };
        public int[] endTurns = { };


        public DataModel(int n, List<long> cap, List<long> dem, long[,] loc,
                        long[,] tw, int[][] pickDel, List<int> starts, List<int> ends,
                        List<int> endTurns,
                        List<int> speed, List<int> cost, long[] cargo)
        {
            this.vehicleNumber = n;
            Starts = starts.ToArray();
            Ends = ends.ToArray();
            this.endTurns = endTurns.ToArray();
            vehicleCapacities = cap.ToArray();
            Demands = dem.ToArray();
            Locations = loc;
            TimeWindows = tw;
            PickupsDeliveries = pickDel;
            vehicleCost = new int[this.vehicleNumber];
            vehicleSpeed = new int[this.vehicleNumber];
            for (int i = 0; i < this.vehicleNumber; i++)
            {
                vehicleCost[i] = cost[i];
                vehicleSpeed[i] = speed[i];
            }
            Cargo = cargo;
        }


        static public DataModel BuildDataModel(long[,] locations_rider, long[,] locations, long[,] tw_rider,
                                  long[,] tw, List<long> demands, List<long> cap_rider,
                                  List<Tuple<string, string>> Pd,
                                  ref BiMap<string, int> map,
                                  List<int> speed,
                                  List<int> cost,
                                  long[] cargo,
                                  List<int> endTurns,
                                  int vehicleNumber
       )

        {
            List<long> new_demands = new List<long>();
            long[,] new_locations = new long[(locations_rider.GetLength(0) * 2) + locations.GetLength(0), 2];
            long[,] new_tw = new long[(tw_rider.GetLength(0) * 2) + tw.GetLength(0), 2];


            List<int> starts = new List<int>();
            List<int> ends = new List<int>();

            List<int> endTurn = new List<int>();


            for (int i = 0; i < vehicleNumber; i++)
            {
                starts.Add(i);
            }

            for (int i = 0; i < locations_rider.GetLength(0); i++)
            {
                new_locations[i, 0] = locations_rider[i, 0];
                new_locations[i, 1] = locations_rider[i, 1];
                new_tw[i, 0] = tw_rider[i, 0];
                new_tw[i, 1] = tw_rider[i, 1];
            }
            int j = locations_rider.GetLength(0);
            for (int i = 0; i < locations.GetLength(0); i++)
            {
                //   if (locations[i, 0] != 0 & locations[i, 1] != 0)
                // {
                new_locations[i + j, 0] = locations[i, 0];
                new_locations[i + j, 1] = locations[i, 1];
                new_tw[i + j, 0] = tw[i, 0];
                new_tw[i + j, 1] = tw[i, 1];
                //  }
            }

            // add demand rider
            for (int i = 0; i < vehicleNumber; i++)
            {
                new_demands.Add(0);
            }

            // add demans  locations
            foreach (var d in demands)
                new_demands.Add(d);

            int[][] mapped_pd = new int[Pd.Count][];
            int n_pair = 0;
            foreach (var pair in Pd)
            {
                mapped_pd[n_pair] = new int[] { map.Forward[pair.Item1], map.Forward[pair.Item2] };
                n_pair++;
            }

            j = locations_rider.GetLength(0) + locations.GetLength(0);
            int rider = 0;
            // Add park
            for (int i = j; i < vehicleNumber + j; i++)
            {
                new_demands.Add(0);
                new_locations[i, 0] = 0;
                new_locations[i, 1] = 0;
                new_tw[i, 0] = tw_rider[rider, 1];
                new_tw[i, 1] = Infinite;
                rider++;
                ends.Add(i);
                map.Add($"park{rider}", i);
            }


            // First Run
            return new DataModel(vehicleNumber, cap_rider, new_demands, new_locations, new_tw, mapped_pd, starts, ends, endTurns, speed, cost, cargo);
        }
    };
}