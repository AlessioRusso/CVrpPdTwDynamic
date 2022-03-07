namespace CVrpPdTwDynamic.Models
{
    public enum StopType
    {
        Delivery,
        ForcedStop,
    }

    public interface IMapRouter
    {
        long GetDistance(Rider op, IRoutableLocation from, IRoutableLocation to);
        long GetDuration(Rider op, IRoutableLocation from, IRoutableLocation to);
    }

    public class MyMapRouter : IMapRouter
    {
        public long GetDistance(Rider op, IRoutableLocation from, IRoutableLocation to)
        {
            var dlat = from.Latitude - (double)to.Latitude;
            var dlon = from.Longitude - (double)to.Longitude;
            return (long)Math.Sqrt(dlat * dlat + dlon * dlon);
        }

        public long GetDuration(Rider op, IRoutableLocation from, IRoutableLocation to)
        {
            return GetDistance(op, from, to) / op.Vehicle;
        }
    }



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

    public class Delivery : RiderStopInfo
    {
        public System.Guid BuyerId;
        public string? guidRider;
        public StopType Type { get; set; } = StopType.Delivery;
        public long ServiceTime { get; internal set; }
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