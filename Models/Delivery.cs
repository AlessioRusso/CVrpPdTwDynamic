using CVrpPdTwDynamic.Enums;

namespace CVrpPdTwDynamic.Models
{

    public class Delivery : RiderStopInfo
    {
        public System.Guid BuyerId;
        public string? guidRider;
        public StopType Type { get; set; } = StopType.Delivery;
        public long ServiceTime { get; internal set; }
    }

}