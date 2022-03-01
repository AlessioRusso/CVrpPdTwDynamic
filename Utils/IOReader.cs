using BidirectionalMap;

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
        public static void ReadStandardInput(string fileName, ref int n, ref List<long> demands,
                              ref long[,] Locations, ref long[,] tw, ref List<Tuple<string, string>> Pd,
                              ref BiMap<string, int> map)
        {


            using (StreamReader input = new StreamReader(fileName))
            {
                List<Tuple<int, int>> coordinates = new List<Tuple<int, int>>();
                List<Tuple<int, int>> times = new List<Tuple<int, int>>();


                // read first row
                string[] splitted;
                splitted = SplitInput(input);
                n = int.Parse(splitted[0]);

                // add riders to map 
                for (int i = 0; i < n; i++)
                {
                    map.Add($"rider{i + 1}", i);
                }


                // start reading locations
                int n_loc = 0;
                while (!input.EndOfStream)
                {
                    splitted = SplitInput(input);

                    if (splitted.Length < 9) break;
                    map.Add(splitted[0], n + n_loc);
                    n_loc++;
                    coordinates.Add(new Tuple<int, int>(int.Parse(splitted[1]), int.Parse(splitted[2])));
                    demands.Add(int.Parse(splitted[3]));
                    times.Add(new Tuple<int, int>(int.Parse(splitted[4]), int.Parse(splitted[5])));
                    if (splitted[7] == "-")
                    {
                        Pd.Add(new Tuple<string, string>(splitted[0], splitted[8]));
                    }
                }

                // initialize time windows and locations
                int n_nodes = coordinates.Count;
                Locations = new long[n_nodes, 2];
                tw = new long[n_nodes, 2];
                int j = 0;

                // build locations 
                foreach (var pair in coordinates)
                {
                    Locations[j, 0] = pair.Item1;
                    Locations[j, 1] = pair.Item2;
                    j++;
                }

                // build time windows
                j = 0;
                foreach (var pair in times)
                {
                    tw[j, 0] = pair.Item1;
                    tw[j, 1] = pair.Item2;
                    j++;
                }

            }
        }

        // The input files follow the "Li & Lim" format
        public static void ReadRiderInput(string fileName, ref List<long> cap_rider,
                              ref long[,] Locations_riders, ref long[,] tw_rider,
                              ref List<int> speed, ref List<int> cost, ref List<int> endsTurn, int vehicleNumber)
        {
            if (fileName == "")
            {
                return;
            }

            using (StreamReader input = new StreamReader(fileName))
            {

                string[] splitted;
                Locations_riders = new long[vehicleNumber, 2];
                tw_rider = new long[vehicleNumber, 2];
                int i = 0;

                while (!input.EndOfStream)
                {
                    splitted = SplitInput(input);

                    if (splitted.Length < 7) break;

                    Locations_riders[i, 0] = int.Parse(splitted[1]);
                    Locations_riders[i, 1] = int.Parse(splitted[2]);

                    cap_rider.Add(int.Parse(splitted[5]));
                    tw_rider[i, 0] = int.Parse(splitted[3]);
                    tw_rider[i, 1] = int.Parse(splitted[4]);
                    endsTurn.Add(int.Parse(splitted[4]));
                    speed.Add(int.Parse(splitted[6]));
                    cost.Add(int.Parse(splitted[7]));
                    i = i + 1;
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