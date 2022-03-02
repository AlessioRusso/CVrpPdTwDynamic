namespace CVrpPdTwDynamic.Models
{
    public class Order
    {
        public Guid id;
        public int ProductCount { get; set; }
        public Shop Shop { get; set; } = null!;
        public ShippingInfo ShippingInfo { get; set; } = null!;
    }
}