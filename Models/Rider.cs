using NetTopologySuite.Geometries;


namespace CVrpPdTwDynamic.Models
{
    public class Rider
    {

        public Rider()
        {
            this.DeliveryFixedFee = 1;
            this.PickupFixedFee = 4;
            this.Vehicle = 1;
        }

        public string guid { get; set; }
        public string Name { get; set; } = null!;
        public string Surname { get; set; } = null!;
        public double KilometricFee { get; set; }
        public long DeliveryFixedFee { get; set; } // cost delivery
        public long PickupFixedFee { get; set; } // cost delivery
        public long Capacity;
        public long Cargo;
        public Point StartLocation { get; set; } = null!;// latitude, longitude
        public long StartTime; // time windows
        public long EndTime; // time windows
        public int EndTurn; // park node (endTurn, infinite) 
        public long Vehicle;
        public string? forcedNode { get; set; } = null!;
    }
}