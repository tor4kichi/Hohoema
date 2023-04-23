using Hohoema.Contracts.Subscriptions;
using Hohoema.Views.Hohoema.Subscription;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Services.Subscriptions;
public sealed class SubscriptionDialogService : ISubscriptionDialogService
{
    async Task<SubscriptionGroupCreateResult> ISubscriptionDialogService.ShowSubscriptionGroupCreateDialogAsync(
        string title, 
        bool isAutoUpdateDefault, 
        bool isAddToQueueeDefault, 
        bool isToastNotificationDefault, 
        bool isShowMenuItemDefault
        )
    {
        var dialog = new EditSubscriptionGroupDialog();
        return await dialog.ShowAsync(
            title: "",
            isAutoUpdateDefault: isAutoUpdateDefault,
            isAddToQueueDefault: isAddToQueueeDefault,
            isToastNotificationDefault: isToastNotificationDefault,
            isShowMenuItemDefault: isShowMenuItemDefault,
            titleValidater: (s) => !string.IsNullOrWhiteSpace(s)
            );
    }
}
