using CVrpPdTwDynamic.Models;

namespace CVrpPdTwDynamic.Services
{
    public class CustomRouter
    {
        private readonly IMapRouter _mapRouter;

        public CustomRouter(IMapRouter mapRouter)
        {
            this._mapRouter = mapRouter;
        }


        public long GetDistance(Rider op, NodeInfo fromNode, NodeInfo toNode)
        {
            // from rider node that is constrained
            if (fromNode is Start && op.forcedNextNode is not null)
                if (op.forcedNextNode != toNode.guid)
                    return DataModel.Infinite;

            if (fromNode is Start && toNode is Idle)
                return 0;

            // from pickup to somewhere else
            if (fromNode is Pickup && fromNode.guid != toNode.guid)
                return op.PickupFixedFee + _mapRouter.GetDistance(op, fromNode, toNode);

            // from pickup to same pickup
            if (fromNode is Pickup && fromNode.guid == toNode.guid)
                return _mapRouter.GetDistance(op, fromNode, toNode); // 0?


            // from delivery to somewhere else
            if (fromNode is Delivery)
            {
                if (toNode is Idle)
                    return ((long)op.DeliveryFixedFee * op.Vehicle);
                return ((long)op.DeliveryFixedFee * op.Vehicle) + _mapRouter.GetDistance(op, fromNode, toNode);
            }

            return _mapRouter.GetDistance(op, fromNode, toNode);
        }

        public long GetDuration(Rider op, NodeInfo fromNode, NodeInfo toNode)
        {

            if (fromNode is Start && toNode is Idle)
                return 0;

            if (fromNode is Pickup fromShop)
            {
                if (fromShop.guid == toNode.guid)
                {
                    // Double pickup in same node
                    return fromShop.SinglePickupServiceTime; // duration is 0
                }
                else
                {
                    // from pickup to somewhere else (pickup or delivery, does not matter)
                    return fromShop.BaseServiceTime + _mapRouter.GetDuration(op, fromNode, toNode);
                }
            }

            // from delivery to somewhere else
            if (fromNode is Delivery fromShippingInfo)
            {
                if (toNode is Idle)
                    return fromShippingInfo.ServiceTime;
                return fromShippingInfo.ServiceTime + _mapRouter.GetDuration(op, fromNode, toNode);
            }

            return _mapRouter.GetDuration(op, fromNode, toNode);
        }
    }

}