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
        public ShippingInfo()
        {
            Type = StopType.Delivery;
        }
        public System.Guid BuyerId;
        public string? guidRider;
        public override StopType Type { get; set; }
    }
}