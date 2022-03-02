namespace CVrpPdTwDynamic.Models
{
    public class Shop : RiderStopInfo
    {
        Shop() { }

        public System.Guid ShopId;
        public override StopType Type => StopType.Pickup;
    }
}