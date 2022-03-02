using BidirectionalMap;
namespace CVrpPdTwDynamic.Models
{
    public class DataModel
    {
        public int vehicleNumber { get; private set; }
        public long[] vehicleCapacities { get; private set; }
        public long[] Cargo { get; private set; }
        public long[] Demands { get; private set; }
        public long[,] Locations { get; private set; }
        public long[,] TimeWindows { get; private set; }
        public int[][] PickupsDeliveries { get; private set; }
        public List<long> MaxDimension = new List<long>();
        public const int ServiceTimeSinglePickup = 1;
        public const int Infinite = 100000000;
        public const int Penalty = 3;
        public int pick_service_time = 5;
        public int delivery_service_time = 1;
        public int[] Starts = { };
        public int[] Ends = { };
        public int[] endTurns = { };

        public DataModel(int vehicleNumber,
                        List<long> Demands,
                        List<int> Starts,
                        List<int> Ends,
                        List<long> vehicleCapacities,
                        List<long> Cargo,
                        List<int> endTurns,
                        long[,] loc,
                        long[,] tw,
                        int[][] pickDel
                    )
        {
            this.vehicleNumber = vehicleNumber;
            this.Demands = Demands.ToArray();
            this.Starts = Starts.ToArray();
            this.Ends = Ends.ToArray();
            this.vehicleCapacities = vehicleCapacities.ToArray();
            this.Cargo = Cargo.ToArray();

            this.endTurns = endTurns.ToArray();
            this.Locations = loc;
            this.TimeWindows = tw;
            this.PickupsDeliveries = pickDel;

            for (int i = 0; i < this.vehicleNumber; i++)
            {
                this.MaxDimension.Add(DataModel.Infinite);
            }
        }


        static public DataModel BuildDataModel(
                                                List<Rider> LogisticOpeartors, long[,] locations,
                                                long[,] tw,
                                                List<long> demandOrders,
                                                List<Tuple<string, string>> Pd,
                                                ref BiMap<string, int> map
        )

        {
            List<long> demands = new List<long>();
            List<long> cargo = new List<long>();
            List<long> vehicleCapacities = new List<long>();
            List<int> endTurns = new List<int>();


            int vehicleNumber = LogisticOpeartors.Count();
            long[,] new_locations = new long[(vehicleNumber * 2) + locations.GetLength(0), 2];
            long[,] new_tw = new long[(vehicleNumber * 2) + tw.GetLength(0), 2];

            List<int> Starts = new List<int>();
            List<int> Ends = new List<int>();

            foreach (var (op, i) in LogisticOpeartors.Select((value, i) => (value, i)))
            {
                new_locations[i, 0] = (long)op.StartLocation.Coordinate.X;
                new_locations[i, 1] = (long)op.StartLocation.Coordinate.Y;
                new_tw[i, 0] = op.StartTime;
                new_tw[i, 1] = op.EndTime;
                vehicleCapacities.Add(op.Capacity);
                Starts.Add(i);
                demands.Add(0);
                cargo.Add(op.Cargo);
                endTurns.Add(op.EndTurn);
            }

            for (int i = 0; i < locations.GetLength(0); i++)
            {
                //   if (locations[i, 0] != 0 & locations[i, 1] != 0)
                // {
                new_locations[i + vehicleNumber, 0] = locations[i, 0];
                new_locations[i + vehicleNumber, 1] = locations[i, 1];
                new_tw[i + vehicleNumber, 0] = tw[i, 0];
                new_tw[i + vehicleNumber, 1] = tw[i, 1];
                //  }
            }

            // add demands Order
            foreach (var d in demandOrders)
                demands.Add(d);

            int[][] mapped_pd = new int[Pd.Count][];
            int n_pair = 0;
            foreach (var pair in Pd)
            {
                mapped_pd[n_pair] = new int[] { map.Forward[pair.Item1], map.Forward[pair.Item2] };
                n_pair++;
            }

            // Add park
            int j = vehicleNumber + locations.GetLength(0);
            foreach (var (op, index) in LogisticOpeartors.Select((value, index) => (value, index)))
            {
                demands.Add(0);
                new_locations[j + index, 0] = 0;
                new_locations[j + index, 1] = 0;
                new_tw[j + index, 0] = op.EndTurn;
                new_tw[j + index, 1] = Infinite;
                Ends.Add(j + index);
                map.Add($"park{index}", j + index);
            }

            // First Run
            return new DataModel(vehicleNumber, demands, Starts, Ends, vehicleCapacities, cargo, endTurns, new_locations, new_tw, mapped_pd);
        }
    };
}