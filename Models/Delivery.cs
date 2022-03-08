namespace CVrpPdTwDynamic.Models
{

    public class Delivery : NodeInfo
    {
        public System.Guid BuyerId;
        public string? guidRider;
        public long ServiceTime { get; internal set; }
    }

}