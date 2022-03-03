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
        public long ServiceTime { get; internal set; }

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
                                                List<Rider> LogisticOpeartors,
                                                List<Order> Orders,
                                                List<Tuple<string, string>> Pd,
                                                ref BiMap<string, int> map,
                                                int past
        )

        {
            List<long> demands = new List<long>();
            List<long> cargo = new List<long>();
            List<long> vehicleCapacities = new List<long>();
            List<int> endTurns = new List<int>();

            int vehicleNumber = LogisticOpeartors.Count();
            int nodesNumber = Orders.Count();
            long[,] new_locations = new long[(vehicleNumber * 2) + (nodesNumber * 2) - past, 2];
            long[,] new_tw = new long[(vehicleNumber * 2) + (nodesNumber * 2) - past, 2];

            List<int> Starts = new List<int>();
            List<int> Ends = new List<int>();

            foreach (var op in LogisticOpeartors)
            {
                var i = map.Forward[op.guid];
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

            int nodes = 0;
            foreach (var order in Orders)
            {
                if (order.ShippingInfo.Type == StopType.Delivery)
                {
                    new_locations[nodes + vehicleNumber, 0] = order.Shop.Latitude;
                    new_locations[nodes + vehicleNumber, 1] = order.Shop.Longitude;
                    new_tw[nodes + vehicleNumber, 0] = order.Shop.StopAfter;
                    new_tw[nodes + vehicleNumber, 1] = order.Shop.StopBefore;
                    demands.Add(order.ProductCount);
                    nodes++;
                    new_locations[nodes + vehicleNumber, 0] = order.ShippingInfo.Latitude;
                    new_locations[nodes + vehicleNumber, 1] = order.ShippingInfo.Longitude;
                    new_tw[nodes + vehicleNumber, 0] = order.ShippingInfo.StopAfter;
                    new_tw[nodes + vehicleNumber, 1] = order.ShippingInfo.StopBefore;
                    demands.Add(-order.ProductCount);
                    nodes++;
                }
                else
                {
                    new_locations[nodes + vehicleNumber, 0] = order.ShippingInfo.Latitude;
                    new_locations[nodes + vehicleNumber, 1] = order.ShippingInfo.Longitude;
                    new_tw[nodes + vehicleNumber, 0] = order.ShippingInfo.StopAfter;
                    new_tw[nodes + vehicleNumber, 1] = order.ShippingInfo.StopBefore;
                    demands.Add(-order.ProductCount);
                    nodes++;
                }
            }

            int[][] mapped_pd = new int[Pd.Count][];
            int n_pair = 0;
            foreach (var pair in Pd)
            {
                mapped_pd[n_pair] = new int[] { map.Forward[pair.Item1], map.Forward[pair.Item2] };
                n_pair++;
            }

            // Add park
            int j = vehicleNumber + (nodesNumber * 2) - past;
            foreach (var (op, index) in LogisticOpeartors.Select((value, index) => (value, index)))
            {
                demands.Add(0);
                new_locations[j + index, 0] = 0;
                new_locations[j + index, 1] = 0;
                new_tw[j + index, 0] = op.EndTurn;
                new_tw[j + index, 1] = Infinite;
                Ends.Add(j + index);
                map.Add($"park{index + 1}", j + index);
            }

            // First Run
            return new DataModel(vehicleNumber, demands, Starts, Ends, vehicleCapacities, cargo, endTurns, new_locations, new_tw, mapped_pd);
        }
    }
}