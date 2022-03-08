using CVrpPdTwDynamic.Enums;

namespace CVrpPdTwDynamic.Models
{
    public class Order
    {
        public Guid id;

        public StopType Type => StopType.PickupAndDelivery;
        public long ProductCount { get; set; }
        public Pickup Pickup { get; set; } = null!;
        public Delivery Delivery { get; set; } = null!;
    }
}