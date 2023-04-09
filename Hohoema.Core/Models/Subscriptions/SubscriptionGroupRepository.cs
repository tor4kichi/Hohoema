using Hohoema.Infra;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Subscriptions;

public sealed class SubscriptionGroupRepository : LiteDBServiceBase<SubscriptionGroup>
{
    public SubscriptionGroupRepository(LiteDatabase liteDatabase) : base(liteDatabase)
    {
        
    }

    public override BsonValue CreateItem(SubscriptionGroup item)
    {
        item.Order = GetNewOrder();
        return base.CreateItem(item);
    }

    private int GetNewOrder()
    {
        try
        {
            return _collection.Max(x => x.Order);
        }
        catch
        {
            return 0;
        }
    }
}
