#nullable enable
using Hohoema.Infra;
using System;

namespace Hohoema.Models.Subscriptions;

public sealed class SubscriptionSettings : FlagsRepositoryBase
{
    public bool IsSubscriptionAutoUpdateEnabled
    {
        get => Read(true);
        set => Save(value);
    }

    public DateTime SubscriptionsLastUpdatedAt
    {
        get => Read(DateTime.Now);
        set => Save(value);
    }

    public TimeSpan SubscriptionsUpdateFrequency
    {
        get => Read(TimeSpan.FromMinutes(60));
        set => Save(value);
    }
}
