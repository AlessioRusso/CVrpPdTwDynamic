using Google.OrTools.ConstraintSolver;
using CVrpPdTwDynamic.Models;

namespace CVrpPdTwDynamic.Utils;
public class Solution
{
    public static void InspectPlan(Dictionary<string, List<NodeInfo>> plan,
                                    Assignment solution)
    {

        Console.WriteLine($"Objective {solution.ObjectiveValue()}:");
        StreamWriter sw = new StreamWriter($"Utils/solution.csv");
        long totalTime = 0;
        foreach (var route in plan)
        {
            foreach (var node in route.Value)
            {
                Console.Write($"{node.guid} Time ({node.PlannedStop.Item1},{node.PlannedStop.Item2}) -> ");
                if (node.IsEnd)
                {
                    Console.Write("O");
                    totalTime += node.PlannedStop.Item1;
                }

                sw.Write(node.guid + " ");
                sw.Write(node.PlannedStop.Item1.ToString() + " ");
                sw.Write(node.PlannedStop.Item2.ToString() + " ");
            }
            sw.WriteLine();
            Console.WriteLine();
        }
        Console.WriteLine($"Total Time:  {totalTime}");
        sw.Close();
    }

    public static Dictionary<string, List<NodeInfo>> GetPlan(DataModel data,
                                                              RoutingModel routing,
                                                              RoutingIndexManager manager,
                                                              Assignment solution
                                                            )
    {

        var plan = new Dictionary<string, List<NodeInfo>>() { };
        RoutingDimension timeDimension = routing.GetMutableDimension("Time");

        foreach (var op in data.LogisticOperators)
        {
            var route = new List<NodeInfo>();
            plan.Add(op.guid, route);

            NodeInfo node = op.StartNode;
            do
            {
                var timeVar = node[timeDimension];
                node.PlannedStop = (solution.Min(timeVar), solution.Max(timeVar));
                route.Add(node);

                if (!node.IsEnd)
                    node = data.GetNode(manager, solution.Value(node.NextVar(routing)));
            } while (!node.IsEnd);

            var endTimeVar = node[timeDimension];
            node.PlannedStop = (solution.Min(endTimeVar), solution.Max(endTimeVar));
            route.Add(node);
        }
        return plan;
    }
}
