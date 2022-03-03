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


        // The input files follow the "Li & Lim" format
        public static void ReadOrders(string fileName, ref List<Order> orders)
        {
            using (StreamReader input = new StreamReader(fileName))
            {
                string[] splitted;
                splitted = SplitInput(input);

                // start reading locations
                while (!input.EndOfStream)
                {
                    splitted = SplitInput(input);
                    Order order = new Order();
                    order.Shop = new Shop();
                    order.Shop.guid = splitted[0];
                    order.ProductCount = (int.Parse(splitted[3]));
                    order.Shop.Latitude = int.Parse(splitted[1]);
                    order.Shop.Longitude = int.Parse(splitted[2]);
                    order.Shop.StopAfter = int.Parse(splitted[4]);
                    splitted = SplitInput(input);
                    order.ShippingInfo = new ShippingInfo();
                    order.ShippingInfo.guid = splitted[0];
                    order.ShippingInfo.Latitude = int.Parse(splitted[1]);
                    order.ShippingInfo.Longitude = int.Parse(splitted[2]);
                    order.ShippingInfo.StopAfter = int.Parse(splitted[4]);
                    orders.Add(order);
                }
            }
        }

        // The input files follow the "Li & Lim" format
        public static void ReadRider(string fileName,
                                          ref List<Rider> LogisticOperators
                                        )
        {
            if (fileName == "")
            {
                return;
            }

            using (StreamReader input = new StreamReader(fileName))
            {

                string[] splitted;

                while (!input.EndOfStream)
                {
                    splitted = SplitInput(input);
                    Rider op = new Rider();
                    op.StartLocation = new NetTopologySuite.Geometries.Point(int.Parse(splitted[1]), int.Parse(splitted[2]));
                    op.Capacity = int.Parse(splitted[5]);
                    op.StartTime = int.Parse(splitted[3]);
                    op.EndTime = int.Parse(splitted[4]);
                    op.EndTurn = int.Parse(splitted[4]);
                    op.Vehicle = int.Parse(splitted[6]);
                    op.Name = splitted[0];
                    op.Surname = splitted[0];
                    op.guid = op.Name + op.Surname;
                    LogisticOperators.Add(op);
                }
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