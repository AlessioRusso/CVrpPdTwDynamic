
namespace CVrpPdTwDynamic.Utils;
public class State
{
    /*

    public static int LocationsCurrentState(
                                    in DataModel data,
                                    ref List<Order> Orders,
                                    in BiMap<string, int> mapNode,
                                    ref BiMap<string, int> CurrentmapNode,
                                    in BiMap<string, string> Pd_map,
                                    List<List<Tuple<string, long, long>>> solution_map,
                                    List<int> present,
                                    ref List<Order> ForcedDeliveries,
                                    ref List<Order> ForcedPickupDeliveries,
                                    ref List<Tuple<string, string>> PickupDeliveries
                                    )
    {
        int n_loc = 0;
        foreach (var (route, i) in solution_map.Select((value, i) => (value, i)))
        {


            for (int j = route.Count - 1; j >= present[i]; j--)
            {
                var node = route[j].Item1;
                if (CheckOrders.isDeliveryIsPastPickUp(node, route, Pd_map, present[i]))
                {
                    Order order = new Order();
                    order.ShippingInfo = new ShippingInfo();
                    order.ShippingInfo.Latitude = data.Locations[mapNode.Forward[node], 0];
                    order.ShippingInfo.Longitude = data.Locations[mapNode.Forward[node], 1];
                    order.ShippingInfo.guid = node;
                    order.ShippingInfo.StopAfter = data.TimeWindows[mapNode.Forward[node], 0];
                    order.ProductCount = -data.Demands[mapNode.Forward[node]];
                    order.ShippingInfo.guidRider = mapNode.Reverse[i];
                    order.ShippingInfo.Type = StopType.ForcedStop;
                    CurrentmapNode.Add(node, n_loc + data.vehicleNumber);
                    n_loc++;
                    ForcedDeliveries.Add(order);
                    Orders.Add(order);
                }
                else if (CheckOrders.isDeliveryIsFuturePickUp(node, route, Pd_map, present[i]))
                {
                    var pickup_node = Pd_map.Reverse[node];
                    Order order = new Order();
                    order.Shop = new Shop();
                    order.Shop.Latitude = data.Locations[mapNode.Forward[pickup_node], 0];
                    order.Shop.Longitude = data.Locations[mapNode.Forward[pickup_node], 1];
                    order.Shop.guid = pickup_node;
                    order.Shop.StopAfter = data.TimeWindows[mapNode.Forward[pickup_node], 0];
                    order.ProductCount = data.Demands[mapNode.Forward[pickup_node]];
                    CurrentmapNode.Add(pickup_node, n_loc + data.vehicleNumber);
                    n_loc++;
                    order.ShippingInfo = new ShippingInfo();
                    order.ShippingInfo.guid = node;
                    order.ShippingInfo.Latitude = data.Locations[mapNode.Forward[node], 0];
                    order.ShippingInfo.Longitude = data.Locations[mapNode.Forward[node], 1];
                    order.ShippingInfo.StopAfter = data.TimeWindows[mapNode.Forward[node], 0];
                    order.ShippingInfo.guidRider = mapNode.Reverse[i];
                    CurrentmapNode.Add(node, n_loc + data.vehicleNumber);
                    n_loc++;
                    if (CheckOrders.isPresentPickUp(pickup_node, route, present[i]))
                    {
                        ForcedPickupDeliveries.Add(order);
                    }
                    PickupDeliveries.Add(Tuple.Create(pickup_node, node));
                    Orders.Add(order);
                }

            }
        }
        return n_loc;
    }

    public static void RiderCurrentState(
                                       in DataModel data,
                                       in List<Rider> PreviousLogisticOperators,
                                       ref List<Rider> CurrentLogisticOperators,
                                       in BiMap<string, int> mapNode,
                                       List<List<Tuple<string, long, long>>> solution_map,
                                       List<int> present
                                       )
    {
        foreach (var (route, i) in solution_map.Select((value, i) => (value, i)))
        {
            var toIndex = mapNode.Forward[route[present[i]].Item1];
            // park state
            Rider op = new Rider();
            op.Name = PreviousLogisticOperators[i].Name;
            op.Surname = PreviousLogisticOperators[i].Surname;
            op.guid = op.Name + op.Surname;
            op.DeliveryFixedFee = PreviousLogisticOperators[i].DeliveryFixedFee;
            op.PickupFixedFee = PreviousLogisticOperators[i].DeliveryFixedFee;
            op.Capacity = PreviousLogisticOperators[i].Capacity;

            if (toIndex == data.Ends[i])
            {
                var prevIndex = mapNode.Forward[route[present[i] - 1].Item1];

                op.StartLocation = new NetTopologySuite.Geometries.Point(data.Locations[prevIndex, 0], data.Locations[prevIndex, 1]);
                op.StartTime = route[present[i] - 1].Item2 + op.DeliveryFixedFee;
                op.EndTime = route[present[i] - 1].Item3 + op.DeliveryFixedFee;
                op.EndTurn = data.endTurns[i];

            }
            else
            {
                op.StartLocation = new NetTopologySuite.Geometries.Point(data.Locations[toIndex, 0], data.Locations[toIndex, 1]);
                op.StartTime = route[present[i]].Item2;
                op.EndTime = route[present[i]].Item3;
                op.EndTurn = data.endTurns[i];
            }
            CurrentLogisticOperators.Add(op);
        }

        foreach (var (route, i) in solution_map.Select((value, i) => (value, i)))
        {
            long actualCargo = 0;
            for (int j = 1; j < route.Count && j < present[i]; j++)
            {
                actualCargo += data.Demands[mapNode.Forward[route[j].Item1]];
            }
            CurrentLogisticOperators[i].Cargo = actualCargo;
        }

    }*/
}
