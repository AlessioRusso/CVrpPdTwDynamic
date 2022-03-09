using Google.OrTools.ConstraintSolver;

namespace CVrpPdTwDynamic.Models;

public interface IRoutableLocation
{
    public string guid { get; }
    public long Latitude { get; }
    public long Longitude { get; }
}

public class NodeInfo : IRoutableLocation
{
    public IntVar this[RoutingDimension dimension] => dimension.CumulVar(Index);
    public IntVar NextVar(RoutingModel routing) => routing.NextVar(Index);
    public IntVar Vehicle(RoutingModel routing) => routing.VehicleVar(Index);

    public void SetRangeConstraints(RoutingDimension timeDimension)
    {
        timeDimension.CumulVar(Index).SetRange(StopAfter, StopBefore);
        if (DelayPenalty > 0)
        {
            timeDimension.SetCumulVarSoftUpperBound(Index, StopAfter, DelayPenalty);
        }
    }

    public virtual bool IsEnd => false;

    public string guid { get; set; } = null!;
    public long Latitude { get; set; }
    public long Longitude { get; set; }
    public long StopAfter { get; set; }
    public long StopBefore => StopAfter + 24 * 60 * 60;
    public long DelayPenalty { get; set; }
    public long Demand { get; set; }
    public string Name { get; set; } = null!;
    public (long, long) PlannedStop { get; set; }
    public long Index { get; set; }
}

public class Start : NodeInfo
{
    public string guidRider;
}

public class Idle : NodeInfo
{
    public string guidRider;

    public override bool IsEnd => true;
}
