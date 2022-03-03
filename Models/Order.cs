namespace CVrpPdTwDynamic.Models
{
    public class Order
    {
        public Guid id;
        public long ProductCount { get; set; }
        public Shop Shop { get; set; } = null!;
        public ShippingInfo ShippingInfo { get; set; } = null!;
    }
}