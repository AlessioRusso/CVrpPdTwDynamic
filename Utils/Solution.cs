using Google.OrTools.ConstraintSolver;
using Google.Protobuf.WellKnownTypes;
using BidirectionalMap;
using CVrpPdTwDynamic.Models;

namespace CVrpPdTwDynamic.Utils
{
    public class Solution
    {
        public static void InspectPlan(Dictionary<string, List<INodeInfo>> plan,
                                        Assignment solution)
        {

            Console.WriteLine($"Objective {solution.ObjectiveValue()}:");
            StreamWriter sw = new StreamWriter($"Utils/solution.csv");
            long totalTime = 0;
            foreach (var riderPlan in plan)
            {
                foreach (var node in riderPlan.Value)
                {
                    Console.Write($"{node.guid} Time ({node.PlannedStop.Item1},{node.PlannedStop.Item2}) -> ");
                    if (node is Idle)
                        totalTime += node.PlannedStop.Item1;

                    sw.Write(node + " ");
                    sw.Write(node.PlannedStop.Item1.ToString() + " ");
                    sw.Write(node.PlannedStop.Item2.ToString() + " ");
                }
                Console.WriteLine();
            }
            Console.WriteLine($"Total Time:  {totalTime}");

            sw.Close();
        }

        public static Dictionary<string, List<INodeInfo>> GetPlan(DataModel data,
                                                                  RoutingModel routing,
                                                                  RoutingIndexManager manager,
                                                                  Assignment solution
                                                                )
        {
            var plan = new Dictionary<string, List<INodeInfo>>() { };
            RoutingDimension timeDimension = routing.GetMutableDimension("Time");

            foreach (var (pair, i) in data.riderMap.Select((value, i) => (value, i)))
            {
                plan.Add(pair.Key, new List<INodeInfo>());
                var index = routing.Start(data.vehicleMap[pair.Key]); //manager.NodeToIndex(data.riderMap[pair.Key]);
                while (routing.IsEnd(index) == false)
                {
                    var timeVar = timeDimension.CumulVar(index);
                    var node = data.nodeMap.Reverse[manager.IndexToNode(index)];
                    node.PlannedStop = (solution.Min(timeVar), solution.Max(timeVar));
                    plan[pair.Key].Add(node);
                    index = solution.Value(routing.NextVar(index));
                }
                var endTimeVar = timeDimension.CumulVar(index);
                var endNode = data.nodeMap.Reverse[manager.IndexToNode(index)];
                endNode.PlannedStop = (solution.Min(endTimeVar), solution.Max(endTimeVar));
                plan[pair.Key].Add(endNode);
            }
            return plan;
        }
    }
}