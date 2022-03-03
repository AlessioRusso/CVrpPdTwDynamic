namespace CVrpPdTwDynamic.Models
{
    public enum StopType
    {
        Pickup,
        Delivery,
        ForcedStop,
    }

    public interface INodeInfo : IRoutableLocation
    {
        public long StopAfter { get; }
        public long StopBefore { get; }
        public long DelayPenalty { get; }
        public long Demand { get; }
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
            return op.Vehicle * GetDistance(op, from, to);
        }
    }

    public interface IRoutableLocation
    {
        public string guid { get; }
        public long Latitude { get; }
        public long Longitude { get; }
    }

    public abstract class RiderStopInfo : INodeInfo
    {
        public string guid { get; set; } = null!;
        public string Name { get; set; } = null!;
        public long StopAfter { get; set; }
        public long StopBefore => StopAfter + 24 * 60 * 60;
        public long Latitude { get; set; }
        public long Longitude { get; set; }
        public abstract StopType Type { get; set; }
    }

    public class ShippingInfo : RiderStopInfo
    {
        public System.Guid BuyerId;
        public string? guidRider;
        public override StopType Type { get; set; } = StopType.Delivery;
        public long ServiceTime { get; internal set; }
    }
}