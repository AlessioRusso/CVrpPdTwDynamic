namespace CVrpPdTwDynamic.Models
{
    public class Order
    {
        public Guid id;
        public long ProductCount { get; set; }
        public Pickup Shop { get; set; } = null!;
        public Delivery ShippingInfo { get; set; } = null!;
    }
}