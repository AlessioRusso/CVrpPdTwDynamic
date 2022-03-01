using CVrpPdTwDynamic.Models;
using BidirectionalMap;
using CVrpPdTwDynamic.Checker;

namespace CVrpPdTwDynamic.Utils
{
    public class State
    {

        public static int LocationsCurrentState(
                                        in DataModel data,
                                        ref long[,] locations_new,
                                        ref long[,] tw_new,
                                        ref List<long> demands_new,
                                        in BiMap<string, int> map,
                                        ref BiMap<string, int> map_new,
                                        in BiMap<string, string> Pd_map,

                                        List<List<Tuple<string, long, long>>> solution_map,
                                        List<int> present,
                                        ref List<List<string>> started_deliveries,
                                        ref List<List<Tuple<string, string>>> pd_constraints,
                                        ref List<Tuple<string, string>> Pd_new
                                        )
        {
            int n_loc = 0;
            foreach (var (route, i) in solution_map.Select((value, i) => (value, i)))
            {
                List<string> single_delivery = new List<string>();
                List<Tuple<string, string>> pick_delivery_constraint = new List<Tuple<string, string>>();

                for (int j = route.Count - 1; j >= present[i]; j--)
                {
                    var node = route[j].Item1;
                    if (CheckOrders.isDeliveryIsPastPickUp(node, route, Pd_map, present[i]))
                    {
                        locations_new[n_loc, 0] = data.Locations[map.Forward[node], 0];
                        locations_new[n_loc, 1] = data.Locations[map.Forward[node], 1];
                        tw_new[n_loc, 0] = data.TimeWindows[map.Forward[node], 0];
                        tw_new[n_loc, 1] = data.TimeWindows[map.Forward[node], 1];
                        demands_new.Add(data.Demands[map.Forward[node]]);
                        map_new.Add(node, n_loc + data.vehicleNumber);
                        n_loc++;
                        single_delivery.Add(node);
                    }
                    else if (CheckOrders.isDeliveryIsFuturePickUp(node, route, Pd_map, present[i]))
                    {
                        var pickup_node = Pd_map.Reverse[node];
                        locations_new[n_loc, 0] = data.Locations[map.Forward[pickup_node], 0];
                        locations_new[n_loc, 1] = data.Locations[map.Forward[pickup_node], 1];
                        tw_new[n_loc, 0] = data.TimeWindows[map.Forward[pickup_node], 0];
                        tw_new[n_loc, 1] = data.TimeWindows[map.Forward[pickup_node], 1];
                        demands_new.Add(data.Demands[map.Forward[pickup_node]]);
                        map_new.Add(pickup_node, n_loc + data.vehicleNumber);
                        n_loc++;
                        locations_new[n_loc, 0] = data.Locations[map.Forward[node], 0];
                        locations_new[n_loc, 1] = data.Locations[map.Forward[node], 1];
                        tw_new[n_loc, 0] = data.TimeWindows[map.Forward[node], 0];
                        tw_new[n_loc, 1] = data.TimeWindows[map.Forward[node], 1];
                        demands_new.Add(data.Demands[map.Forward[node]]);
                        map_new.Add(node, n_loc + data.vehicleNumber);
                        n_loc++;
                        if (CheckOrders.isPresentPickUp(pickup_node, route, present[i]))
                        {
                            pick_delivery_constraint.Add(Tuple.Create(pickup_node, node));
                        }
                        Pd_new.Add(Tuple.Create(pickup_node, node));
                    }

                }
                started_deliveries.Add(single_delivery);
                pd_constraints.Add(pick_delivery_constraint);
            }
            return n_loc;


        }

        public static void RiderCurrentState(
                                           in DataModel data,
                                           ref long[,] locations_rider_new,
                                           ref long[,] tw_rider_new,
                                           ref long[] cargo_new,
                                           in BiMap<string, int> map,
                                           ref BiMap<string, int> map_new,
                                           List<List<Tuple<string, long, long>>> solution_map,
                                           List<int> present
                                           )
        {
            foreach (var (route, i) in solution_map.Select((value, i) => (value, i)))
            {
                var toIndex = map.Forward[route[present[i]].Item1];
                // park state
                if (toIndex == data.Ends[i])
                {
                    var prevIndex = map.Forward[route[present[i] - 1].Item1];
                    locations_rider_new[i, 0] = data.Locations[prevIndex, 0];
                    locations_rider_new[i, 1] = data.Locations[prevIndex, 1];
                    tw_rider_new[i, 0] = route[present[i] - 1].Item2 + DataModel.CostDelivery;
                    tw_rider_new[i, 1] = route[present[i] - 1].Item3 + DataModel.CostDelivery;
                }
                else
                {
                    locations_rider_new[i, 0] = data.Locations[toIndex, 0];
                    locations_rider_new[i, 1] = data.Locations[toIndex, 1];
                    tw_rider_new[i, 0] = route[present[i]].Item2;
                    tw_rider_new[i, 1] = route[present[i]].Item3;
                }
            }

            foreach (var (route, i) in solution_map.Select((value, i) => (value, i)))
            {
                map_new.Add($"rider{i + 1}", i);
                long actualCargo = 0;
                for (int j = 1; j < route.Count && j < present[i]; j++)
                {
                    actualCargo += data.Demands[map.Forward[route[j].Item1]];
                }
                cargo_new[i] = actualCargo;
            }

        }
    }
}
