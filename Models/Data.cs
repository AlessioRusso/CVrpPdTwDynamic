using BidirectionalMap;
using CVrpPdTwDynamic.Enums;

namespace CVrpPdTwDynamic.Models
{
    public class DataModel
    {

        public List<long> MaxDimension = new List<long>();
        public const int Infinite = 100000000;
        public List<int> Starts;
        public List<int> Ends;
        public List<Rider> LogisticOperators;
        public List<Order> OrdersAndForced;
        public Dictionary<string, int> riderMap;
        public BiMap<INodeInfo, int> nodeMap;
        public Dictionary<string, int> vehicleMap;

        public DataModel(List<Rider> LogisticOperators, List<Order> OrdersAndForced)
        {
            this.Starts = new List<int>();
            this.Ends = new List<int>();
            this.nodeMap = new BiMap<INodeInfo, int>();
            this.riderMap = new Dictionary<string, int>();
            this.vehicleMap = new Dictionary<string, int>();


            this.LogisticOperators = LogisticOperators;
            this.OrdersAndForced = OrdersAndForced;
            this.Starts = new List<int>();

            foreach (var order in this.OrdersAndForced)
            {
                if (order.ShippingInfo.Type == StopType.ForcedStop)
                    this.nodeMap.Add(order.ShippingInfo, this.nodeMap.Count());
                else
                {
                    this.nodeMap.Add(order.Shop, this.nodeMap.Count());
                    this.nodeMap.Add(order.ShippingInfo, this.nodeMap.Count());
                }
            }

            foreach (var rider in LogisticOperators)
            {
                var endNode = new Idle()
                {
                    guid = "idle" + " " + rider.Name,
                    Latitude = 0,
                    Longitude = 0,
                    DelayPenalty = DataModel.Infinite,
                    Demand = 0,
                    StopAfter = rider.EndTurn,
                    guidRider = rider.guid,
                };
                this.Ends.Add(this.nodeMap.Count());
                this.nodeMap.Add(endNode, this.nodeMap.Count());
            }

            foreach (var rider in LogisticOperators)
            {
                var startNode = new Start()
                {
                    guid = rider.guid,
                    guidRider = rider.guid,
                    Latitude = (long)rider.StartLocation.Coordinate.X,
                    Longitude = (long)rider.StartLocation.Coordinate.Y,
                    DelayPenalty = DataModel.Infinite,
                    StopAfter = rider.StartTime,
                    Demand = 0,
                };
                this.Starts.Add(this.nodeMap.Count());
                this.riderMap.Add(rider.guid, this.nodeMap.Count());
                this.nodeMap.Add(startNode, this.nodeMap.Count());
                this.vehicleMap.Add(rider.guid, this.vehicleMap.Count());
                this.MaxDimension.Add(DataModel.Infinite);
            }


        }
    }
}