using Google.OrTools.ConstraintSolver;
using Google.Protobuf.WellKnownTypes;
using CVrpPdTwDynamic.Models;
using CVrpPdTwDynamic.Utils;

public class VrpPickupDelivery
{
    public static void Main(String[] args)
    {

        var LogisticOperators = IOReader.ReadRider(args[1]);

        var OrdersAndForced = IOReader.ReadOrders(args[0]);

        DataModel data = new DataModel(LogisticOperators, OrdersAndForced);

        RoutingIndexManager manager = new RoutingIndexManager(
                data.nodeMap.Count(),
                data.LogisticOperators.Count(),
                data.Starts.ToArray(),
                data.Ends.ToArray()
            );

        RoutingModel routing = Routing.CreateRoutingModel(manager, data, new MyMapRouter());
        RoutingSearchParameters searchParameters =
            operations_research_constraint_solver.DefaultRoutingSearchParameters();
        searchParameters.TimeLimit = new Duration { Seconds = 100 };
        searchParameters.FirstSolutionStrategy = FirstSolutionStrategy.Types.Value.PathCheapestArc;
        Assignment solution = routing.SolveWithParameters(searchParameters);

        var plan = Solution.GetPlan(data, routing, manager, solution);
        Solution.InspectPlan(plan, solution);

    }
}
