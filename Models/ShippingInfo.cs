namespace CVrpPdTwDynamic.Models
{
    public enum StopType
    {
        Pickup,
        Delivery,
        ForcedStop,
    }

    public abstract class RiderStopInfo
    {
        public System.Guid Id;
        public string Name { get; set; } = null!;
        public long StopAfter { get; set; }
        public long StopBefore => StopAfter + 24 * 60 * 60;
        public long Latitude { get; set; }
        public long Longitude { get; set; }

        public abstract StopType Type { get; }
    }

    public class ShippingInfo : RiderStopInfo
    {
        public System.Guid BuyerId;
        public string? RiderId;
        public override StopType Type => StopType.Delivery;
    }
}