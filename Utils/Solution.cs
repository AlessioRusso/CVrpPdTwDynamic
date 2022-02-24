using Google.OrTools.ConstraintSolver;
using Google.Protobuf.WellKnownTypes;
using BidirectionalMap;
using CVrpPdTwDynamic.Models;

namespace CVrpPdTwDynamic.Utils
{
    public class Solution
    {
        public static List<List<Tuple<string, long, long>>> PrintSolution(in DataModel data, in RoutingModel routing, in RoutingIndexManager manager,
                        in Assignment solution, in BiMap<string, int> map, int run)
        {
            List<List<Tuple<string, long, long>>> solution_map = new List<List<Tuple<string, long, long>>>() { };
            Console.WriteLine($"Objective {solution.ObjectiveValue()}:");
            StreamWriter sw = new StreamWriter($"Utils/solution{run}.csv");
            RoutingDimension timeDimension = routing.GetMutableDimension("Time");
            long totalTime = 0;
            for (int i = 0; i < data.vehicleNumber; ++i)
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
            Console.WriteLine("------------------------------------------------");

            sw.Close();
            return solution_map;
        }
    }
}