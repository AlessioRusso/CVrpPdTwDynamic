using BidirectionalMap;

namespace CVrpPdTwDynamic.Checker
{
    public class CheckOrders
    {
        public static bool isDeliveryIsPastPickUp(string node, List<Tuple<string, long, long>> route,
                                BiMap<string, string> Pd_map, int present)
        {
            string? pick = null;
            try
            {
                pick = Pd_map.Reverse[node];
            }
            catch { }
            if (pick == null) return false;
            for (int i = 0; i < route.Count; i++)
            {
                if (route[i].Item1.Equals(pick) & i >= present)
                {
                    return false;
                }
            }

            return true;
        }

        public static bool isDeliveryIsFuturePickUp(string node, List<Tuple<string, long, long>> route,
                                    BiMap<string, string> Pd_map, int present)
        {
            string? pick = null;
            try
            {
                pick = Pd_map.Reverse[node];
            }
            catch { }
            if (pick == null) return false;
            for (int i = 0; i < route.Count; i++)
            {
                if (route[i].Item1.Equals(pick) & i < present)
                {
                    return false;
                }
            }

            return true;
        }


        public static bool isPresentPickUp(string node, List<Tuple<string, long, long>> route,
                                    int present)
        {
            return route[present].Item1.Equals(node);
        }

    }
}