using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Contracts.Subscriptions;
public interface ISubscriptionDialogService
{
    Task<SubscriptionGroupCreateResult> ShowSubscriptionGroupCreateDialogAsync(
        string title,
        bool isAutoUpdateDefault,
        bool isAddToQueueeDefault,
        bool isToastNotificationDefault,
        bool isShowMenuItemDefault
        );
}

public readonly struct SubscriptionGroupCreateResult
{
    public readonly bool IsSuccess { get; init; }
    public readonly string Title { get; init; }
    public readonly bool IsAutoUpdate { get; init; }
    public readonly bool IsAddToQueue { get; init; }
    public readonly bool IsToastNotification { get; init; }
    public readonly bool IsShowMenuItem { get; init; }
}