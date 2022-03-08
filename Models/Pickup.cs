namespace CVrpPdTwDynamic.Models
{
    public class Pickup : NodeInfo
    {
        public System.Guid ShopId;
        public long SinglePickupServiceTime { get; set; }
        public long BaseServiceTime { get; set; }
    }
}