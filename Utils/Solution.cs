using Google.OrTools.ConstraintSolver;
using Google.Protobuf.WellKnownTypes;
using BidirectionalMap;
using CVrpPdTwDynamic.Models;

namespace CVrpPdTwDynamic.Utils
{
    public class Solution
    {

        public static int VisitedLocations(List<int> present)
        {
            int past = 0;
            for (int i = 0; i < present.Count; i++)
            {
                if (present[i] != 0)
                    past += present[i] - 1;
            }
            return past;
        }

        public static List<List<Tuple<INodeInfo, long, long>>> PrintSolution(in DataModel data, in RoutingModel routing, in RoutingIndexManager manager,
                        in Assignment solution)
        {
            var solution_map = new List<List<Tuple<INodeInfo, long, long>>>() { };
            Console.WriteLine($"Objective {solution.ObjectiveValue()}:");
            StreamWriter sw = new StreamWriter($"Utils/solution.csv");
            RoutingDimension timeDimension = routing.GetMutableDimension("Time");
            long totalTime = 0;
            for (int i = 0; i < data.LogisticOperators.Count(); ++i)
            {
                var solution_map_rider = new List<Tuple<INodeInfo, long, long>>() { };
                Console.WriteLine();
                var index = routing.Start(i);
                while (routing.IsEnd(index) == false)
                {
                    var timeVar = timeDimension.CumulVar(index);
                    var node = data.nodeMap.Reverse[manager.IndexToNode(index)];
                    solution_map_rider.Add(Tuple.Create(node, solution.Min(timeVar), solution.Max(timeVar)));
                    Console.Write($"{node.guid} Time ({solution.Min(timeVar)},{solution.Max(timeVar)}) -> ");
                    sw.Write(node + " ");
                    sw.Write(solution.Min(timeVar).ToString() + " ");
                    sw.Write(solution.Max(timeVar).ToString() + " ");
                    index = solution.Value(routing.NextVar(index));
                }
                var endTimeVar = timeDimension.CumulVar(index);

                solution_map_rider.Add(Tuple.Create(data.nodeMap.Reverse[manager.IndexToNode(index)], solution.Min(endTimeVar), solution.Max(endTimeVar)));

                Console.WriteLine($"{data.nodeMap.Reverse[manager.IndexToNode(index)].guid} Time({solution.Min(endTimeVar)},{ solution.Max(endTimeVar)})");
                Console.WriteLine("Time of the route: {0}", solution.Min(endTimeVar));
                totalTime += solution.Min(endTimeVar);
                sw.Write(data.nodeMap.Reverse[manager.IndexToNode(index)]);
                sw.WriteLine();
                solution_map.Add(solution_map_rider);
            }
            Console.WriteLine("------------------------------------------------");

            sw.Close();
            return solution_map;
        }
    }
}