namespace CVrpPdTwDynamic.Models
{

    public interface IRoutableLocation
    {
        public string guid { get; }
        public long Latitude { get; }
        public long Longitude { get; }
    }

    public interface INodeInfo : IRoutableLocation
    {
        public long StopAfter { get; }
        public long StopBefore { get; }
        public long DelayPenalty { get; }
        public long Demand { get; }
        public (long, long) PlannedStop { get; set; }
    }

    public abstract class RiderStopInfo : INodeInfo
    {
        public string guid { get; set; } = null!;
        public long Latitude { get; set; }
        public long Longitude { get; set; }
        public long StopAfter { get; set; }
        public long StopBefore => StopAfter + 24 * 60 * 60;
        public long DelayPenalty { get; set; }
        public long Demand { get; set; }
        public string Name { get; set; } = null!;
        public (long, long) PlannedStop { get; set; }
    }

    public class Start : RiderStopInfo
    {
        public string guidRider;
    }

    public class Idle : RiderStopInfo
    {
        public string guidRider;
    }

}