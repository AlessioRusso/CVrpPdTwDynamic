using BidirectionalMap;
using CVrpPdTwDynamic.Models;

namespace CVrpPdTwDynamic.Utils
{
    public class IOReader
    {
        public static string[] SplitInput(StreamReader input)
        {
            string? line = input.ReadLine();
            if (line == null) return new string[0];
            return line.Split(new[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
        }

        public static List<Order> ReadOrders(string fileName)
        {
            List<Order> orders = new();
            ReadOrders(fileName, orders);
            return orders;
        }

        public static void ReadOrders(string fileName, List<Order> orders)
        {
            using StreamReader input = new StreamReader(fileName);
            string[] splitted = SplitInput(input);

            // start reading locations
            while (!input.EndOfStream)
            {
                splitted = SplitInput(input);
                Order order = new Order();
                order.ProductCount = (int.Parse(splitted[3]));

                order.Pickup = new Pickup()
                {
                    guid = splitted[0],
                    Latitude = int.Parse(splitted[1]),
                    Longitude = int.Parse(splitted[2]),
                    StopAfter = int.Parse(splitted[4]),
                    DelayPenalty = 4,
                    BaseServiceTime = 1,
                    Demand = order.ProductCount,
                };

                splitted = SplitInput(input);
                order.Delivery = new Delivery()
                {
                    guid = splitted[0],
                    Latitude = int.Parse(splitted[1]),
                    Longitude = int.Parse(splitted[2]),
                    StopAfter = int.Parse(splitted[4]),
                    DelayPenalty = 4,
                    ServiceTime = 1,
                    Demand = -order.ProductCount,
                };

                orders.Add(order);
            }
        }

        public static List<Rider> ReadRider(string fileName)
        {
            var riders = new List<Rider>();
            ReadRider(fileName, riders);
            return riders;
        }

        public static void ReadRider(string fileName, List<Rider> LogisticOperators)
        {

            using StreamReader input = new StreamReader(fileName);
            string[] splitted;

            while (!input.EndOfStream)
            {
                splitted = SplitInput(input);
                Rider op = new Rider()
                {
                    Name = splitted[0],
                    Surname = splitted[0],
                    StartLocation = new NetTopologySuite.Geometries.Point(int.Parse(splitted[1]), int.Parse(splitted[2])),
                    StartTime = int.Parse(splitted[3]),
                    EndTime = int.Parse(splitted[4]),
                    EndTurn = int.Parse(splitted[4]),
                    Capacity = int.Parse(splitted[5]),
                    Vehicle = int.Parse(splitted[6]),
                };
                op.guid = op.Name + op.Surname;
                LogisticOperators.Add(op);
            }
        }

        public static void SaveWorldLocations(long[,] ridersLocations,
                               long[,] pdLocations,
                               BiMap<string, int> map,
                               int run)
        {
            StreamWriter swRider = new StreamWriter($"locationsRider{run}.csv");
            var rider = ridersLocations.GetLength(0);
            for (int i = 0; i < ridersLocations.GetLength(0); i++)
            {
                swRider.Write(map.Reverse[i] + " ");
                swRider.Write(ridersLocations[i, 0] + " " + ridersLocations[i, 1]);
                swRider.WriteLine();
            }
            swRider.Close();

            StreamWriter swPd = new StreamWriter($"locationsPd{run}.csv");

            for (int i = 0; i < pdLocations.GetLength(0); i++)
            {
                swPd.Write(map.Reverse[i + rider] + " ");
                swPd.Write(pdLocations[i, 0] + " " + pdLocations[i, 1]);
                swPd.WriteLine();
            }
            swPd.Close();
        }
    }

}