using CVrpPdTwDynamic.Enums;
using Google.OrTools.ConstraintSolver;

namespace CVrpPdTwDynamic.Models;
public class DataModel
{

    public List<long> MaxDimension = new List<long>();
    public const int Infinite = 100000000;
    public List<int> Starts = new();
    public List<int> Ends = new();
    public List<Rider> LogisticOperators;
    public List<Order> OrdersAndForced;
    public Dictionary<string, int> riderMap = new();
    public List<NodeInfo> Nodes = new();


    public NodeInfo GetNode(RoutingIndexManager manager, long index) => this.Nodes[manager.IndexToNode(index)];
    public DataModel(List<Rider> LogisticOperators, List<Order> OrdersAndForced)
    {

        this.LogisticOperators = LogisticOperators;
        this.OrdersAndForced = OrdersAndForced;

        foreach (var order in this.OrdersAndForced)
        {
            if (order.Type == StopType.DeliveryOnly)
                this.Nodes.Add(order.Delivery);
            else
            {
                this.Nodes.Add(order.Pickup);
                this.Nodes.Add(order.Delivery);
            }
        }

        foreach (var rider in LogisticOperators)
        {
            rider.EndNode = new Idle()
            {
                guid = "idle" + " " + rider.Name,
                Latitude = 0,
                Longitude = 0,
                DelayPenalty = DataModel.Infinite,
                Demand = 0,
                StopAfter = rider.EndTurn,
                guidRider = rider.guid,
            };
            this.Ends.Add(this.Nodes.Count());
            this.Nodes.Add(rider.EndNode);
        }

        foreach (var rider in LogisticOperators)
        {
            rider.StartNode = new Start()
            {
                guid = rider.guid,
                guidRider = rider.guid,
                Latitude = (long)rider.StartLocation.Coordinate.X,
                Longitude = (long)rider.StartLocation.Coordinate.Y,
                DelayPenalty = 0,
                StopAfter = rider.StartTime,
                Demand = 0,
            };
            this.Starts.Add(this.Nodes.Count());
            this.riderMap.Add(rider.guid, this.riderMap.Count());
            this.Nodes.Add(rider.StartNode);
            this.MaxDimension.Add(DataModel.Infinite);
        }
    }
}
