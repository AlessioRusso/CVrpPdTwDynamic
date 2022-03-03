namespace CVrpPdTwDynamic.Models
{
    public class Shop : RiderStopInfo
    {
        public Shop()
        {
            Type = StopType.Pickup;
        }

        public System.Guid ShopId;
        public override StopType Type { get; set; }
        public long SinglePickupServiceTime { get; set; }
        public long BaseServiceTime { get; set; }
    }
}