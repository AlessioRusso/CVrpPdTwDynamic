namespace CVrpPdTwDynamic.Models
{
    public class Pickup : RiderStopInfo
    {
        public System.Guid ShopId;
        public long SinglePickupServiceTime { get; set; }
        public long BaseServiceTime { get; set; }
    }
}