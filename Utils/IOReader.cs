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
        public static void ReadStandardInput(string fileName, ref List<long> demands,
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
                int n = int.Parse(splitted[0]);

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
        public static void ReadRiderInput(string fileName,
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
                    if (splitted.Length < 7) break;
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