using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NicoPlayerHohoema.Models.Helpers;
using NicoPlayerHohoema.Services.Helpers;

namespace NicoPlayerHohoema.Commands.Subscriptions
{
    public sealed class CreateSubscriptionGroupCommand : DelegateCommandBase
    {
        protected override bool CanExecute(object parameter)
        {
            return true;
        }

        protected override async void Execute(object parameter)
        {
            var dialog = Commands.HohoemaCommnadHelper.GetDialogService();

            var groupName = await dialog.GetTextAsync("SubscriptionGroup_Create".ToCulturelizeString(), "", validater: (s) => !string.IsNullOrWhiteSpace(s));

            if (groupName == null) { return; }

            var subscription = new Models.Subscription.Subscription(Guid.NewGuid(), groupName);
            if (parameter is Models.Subscription.SubscriptionSource source)
            {
                subscription.Sources.Add(source);
            }

            Models.Subscription.SubscriptionManager.Instance.Subscriptions.Add(subscription);
        }
    }
}
