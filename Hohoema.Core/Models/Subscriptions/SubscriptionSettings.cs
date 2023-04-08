using Hohoema.Infra;
using System;

namespace Hohoema.Models.Subscriptions;

[Obsolete]
public sealed class SubscriptionSettings : FlagsRepositoryBase
{
    [Obsolete]
    public bool IsSubscriptionAutoUpdateEnabled
    {
        get => Read(true);
        set => Save(value);
    }

    [Obsolete]
    public DateTime SubscriptionsLastUpdatedAt
    {
        get => Read(DateTime.Now);
        set => Save(value);
    }

    [Obsolete]
    public TimeSpan SubscriptionsUpdateFrequency
    {
        get => Read(TimeSpan.FromMinutes(60));
        set => Save(value);
    }



    [Obsolete]
    public bool IsSortWithSubscriptionUpdated
    {
        get => Read(true);
        set => Save(value);
    }
}
