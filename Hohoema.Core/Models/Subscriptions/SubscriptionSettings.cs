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

    public bool Default_IsAutoUpdate
    {
        get => Read(true);
        set => Save(value);
    }

    public bool Default_IsAddToQueue
    {
        get => Read(true);
        set => Save(value);
    }

    public bool Default_IsToastNotification
    {
        get => Read(true);
        set => Save(value);
    }

    public bool Default_IsShowMenuItem
    {
        get => Read(true);
        set => Save(value);
    }
}
