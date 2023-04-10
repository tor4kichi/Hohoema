#nullable enable
using LiteDB;
using System;
using System.Collections.Generic;

namespace Hohoema.Models.Subscriptions;

public sealed class SubscriptionSourceEntity
{
    [BsonId]
    public ObjectId Id { get; internal set; }
    public int SortIndex { get; set; }
    public string Label { get; set; } = string.Empty;
    public SubscriptionSourceType SourceType { get; set; }
    public string SourceParameter { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public DateTime LastUpdateAt { get; set; } = DateTime.MinValue;
    
    [BsonRef]
    public SubscriptionGroup? Group { get; set; }
}

public sealed class SubscriptionGroupComparer : IEqualityComparer<SubscriptionGroup>
{
    public static readonly SubscriptionGroupComparer Default = new SubscriptionGroupComparer();
    public bool Equals(SubscriptionGroup x, SubscriptionGroup y)
    {
        return x.Id.Equals(y.Id);
    }

    public int GetHashCode(SubscriptionGroup obj)
    {
        return obj.Id.GetHashCode();
    }
}

public sealed class SubscriptionGroup : IComparable<SubscriptionGroup>, IEquatable<SubscriptionGroup>
{
    [BsonId]
    public ObjectId Id { get; }
    
    public string Name { get; set; } = string.Empty;

    public int Order { get; set; } = 0;

    [BsonIgnore]
    public bool IsInvalidId => Id == ObjectId.Empty;

    [BsonCtor]
    public SubscriptionGroup(ObjectId _id, string name)
    {
        Id = _id;
        Name = name;
    }

    public SubscriptionGroup(string name)
    {
        Id = ObjectId.NewObjectId();
        Name = name;
    }

    public int CompareTo(SubscriptionGroup other)
    {
        return this.Id.CompareTo(other.Id);
    }

    public bool Equals(SubscriptionGroup? other)
    {        
        return this.Id.Equals(other.Id);
    }
}


public enum SubscriptionSourceType
{
    Mylist,
    User,
    Channel,
    Series,
    SearchWithKeyword,
    SearchWithTag,
}
