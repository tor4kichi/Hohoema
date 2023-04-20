#nullable enable
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Hohoema.Contracts.Subscriptions;
using Hohoema.Infra;
using LiteDB;
using Microsoft.Toolkit.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Hohoema.Models.Subscriptions;



public sealed class SubscriptionRegistrationRepository : LiteDBServiceBase<Subscription>
{    
    static SubscriptionRegistrationRepository()
    {
        BsonMapper.Global.RegisterType(x => x.AsPrimitive(), x => new SubscriptionId(x.AsObjectId));
    }

    public SubscriptionRegistrationRepository(LiteDatabase database)
        : base(database, "SubscriptionSourceEntity" /* 最初に利用したテーブル名を維持したい */ )
    {
        _collection.EnsureIndex(x => x.SortIndex);
        _collection.EnsureIndex(x => x.Group!.GroupId);
    }

    public void ClearAll()
    {
        _collection.DeleteAll();
    }

    public bool TryGetSubscriptionGroup(SubscriptionSourceType sourceType, string id, out SubscriptionGroup? outGroup)
    {
        outGroup = _collection.Include(x => x.Group).FindOne(x => x.SourceType == sourceType && x.SourceParameter == id)?.Group;
        return outGroup != null;
    }

    public bool TryGetSubscriptionGroup(SubscriptionSourceType sourceType, string id, out Subscription outSourceEntity, out SubscriptionGroup? outGroup)
    {
        outSourceEntity = _collection.Include(x => x.Group).FindOne(x => x.SourceType == sourceType && x.SourceParameter == id);
        outGroup = outSourceEntity?.Group;
        return outGroup != null;
    }


    public bool IsExist(Subscription other)
    {
        return _collection.Exists(x => x.SourceType == other.SourceType && x.SourceParameter == other.SourceParameter);
    }

    public override List<Subscription> ReadAllItems()
    {
        return _collection.Include(x => x.Group).FindAll().ToList();
    }

    public override BsonValue CreateItem(Subscription entity)
    {
        if (IsExist(entity))
        {
            _collection.Update(entity);
            return entity.SubscriptionId.AsPrimitive();
        }
        else
        {
            // Note: エンティティが登録されていない状態で .Max() を呼ぶと
            //       InvalidOperationException が発生する (LiteDb 5.0.16)
            try
            {
                entity.SortIndex = _collection.Max(x => x.SortIndex) + 1;
            }
            catch
            {
                entity.SortIndex = 0;
            }
            BsonValue result = base.CreateItem(entity);
            return result;
        }
    }

    internal bool DeleteItem(SubscriptionId subscriptionId)
    {
        return base.DeleteItem(subscriptionId.AsPrimitive());
    }

    internal Subscription FindById(SubscriptionId id)
    {
        return _collection.Include(x => x.Group).FindById(id.AsPrimitive());
    }

    internal IEnumerable<Subscription> Find(SubscriptionGroupId groupId)
    {
        return _collection.Include(x => x.Group).Find(x => x.Group!.GroupId == groupId).OrderBy(x => x.SortIndex);
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


public sealed class Subscription
{
    [BsonCtor]
    public Subscription(
        SubscriptionId _id, 
        int sortIndex, 
        string label, 
        SubscriptionSourceType sourceType, 
        string sourceParameter, 
        bool isAutoUpdateEnabled, 
        bool isAddToQueueWhenUpdated, 
        SubscriptionGroup? group
        )
    {
        SubscriptionId = _id;
        SortIndex = sortIndex;
        Label = label;
        SourceType = sourceType;
        SourceParameter = sourceParameter;
        IsAutoUpdateEnabled = isAutoUpdateEnabled;
        IsAddToQueueWhenUpdated = isAddToQueueWhenUpdated;
        Group = group;
    }

    public Subscription(SubscriptionId subscriptionId)
    {
        SubscriptionId = subscriptionId;
    }

    [BsonId]
    public SubscriptionId SubscriptionId { get; internal set; }
    public int SortIndex { get; set; }
    public string Label { get; set; } = string.Empty;
    public SubscriptionSourceType SourceType { get; set; }
    public string SourceParameter { get; set; } = string.Empty;
    public bool IsAutoUpdateEnabled { get; set; } = true;
    public bool IsAddToQueueWhenUpdated { get; set; } = false;

    [BsonRef]
    public SubscriptionGroup? Group { get; set; }
}
