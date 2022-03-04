using BidirectionalMap;
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
        public Dictionary<string, int>? riderMap;
        public BiMap<INodeInfo, int> nodeMap;


        public DataModel(List<Rider> LogisticOperators, List<Order> OrdersAndForced)
        {
            this.Starts = new List<int>();
            this.Ends = new List<int>();

            this.LogisticOperators = LogisticOperators;
            this.OrdersAndForced = OrdersAndForced;
            this.riderMap = LogisticOperators
              .Select((op, i) => (op.guid, i))
              .ToDictionary(pair => pair.guid, pair => pair.i);
            this.Starts = riderMap.Select((pair) => pair.Value).ToList();

            this.nodeMap = new BiMap<INodeInfo, int>();
            foreach (var rider in LogisticOperators)
            {
                var startNode = new StartInfo();
                startNode.guid = rider.guid;
                startNode.Latitude = (long)rider.StartLocation.Coordinate.X;
                startNode.Longitude = (long)rider.StartLocation.Coordinate.Y;
                startNode.DelayPenalty = DataModel.Infinite;
                startNode.StopAfter = rider.StartTime;
                startNode.Demand = 0;
                this.nodeMap.Add(startNode, this.riderMap[startNode.guid]);
                this.MaxDimension.Add(DataModel.Infinite);
            }

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
                var endNode = new IdleInfo();
                endNode.guid = rider.guid;
                endNode.Latitude = (long)rider.StartLocation.Coordinate.X;
                endNode.Longitude = (long)rider.StartLocation.Coordinate.Y;
                endNode.DelayPenalty = DataModel.Infinite;
                endNode.Demand = 0;
                endNode.StopAfter = rider.EndTurn;
                this.Ends.Add(this.nodeMap.Count());
                this.nodeMap.Add(endNode, this.nodeMap.Count());
            }

        }

    }
}