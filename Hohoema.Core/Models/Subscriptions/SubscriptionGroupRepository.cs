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



public sealed class SubscriptionGroupComparer : IEqualityComparer<SubscriptionGroup>
{
    public static readonly SubscriptionGroupComparer Default = new SubscriptionGroupComparer();
    public bool Equals(SubscriptionGroup x, SubscriptionGroup y)
    {
        return x.GroupId.Equals(y.GroupId);
    }

    public int GetHashCode(SubscriptionGroup obj)
    {
        return obj.GroupId.GetHashCode();
    }
}

public sealed class SubscriptionGroup : IComparable<SubscriptionGroup>, IEquatable<SubscriptionGroup>
{
    [BsonId]
    public ObjectId GroupId { get; }

    public string Name { get; set; } = string.Empty;

    public int Order { get; set; } = 0;

    [BsonIgnore]
    public bool IsInvalidId => GroupId == ObjectId.Empty;

    [BsonCtor]
    public SubscriptionGroup(ObjectId _id, string name)
    {
        GroupId = _id;
        Name = name;
    }

    public SubscriptionGroup(string name)
    {
        GroupId = ObjectId.NewObjectId();
        Name = name;
    }

    public int CompareTo(SubscriptionGroup other)
    {
        return this.GroupId.CompareTo(other.GroupId);
    }

    public bool Equals(SubscriptionGroup? other)
    {
        return this.GroupId.Equals(other.GroupId);
    }
}
