using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NicoPlayerHohoema.Models.Helpers;
using NicoPlayerHohoema.Services.Helpers;
using NicoPlayerHohoema.Models.Subscription;
using NicoPlayerHohoema.Services;

namespace NicoPlayerHohoema.Commands.Subscriptions
{
    public sealed class CreateSubscriptionGroupCommand : DelegateCommandBase
    {
        public CreateSubscriptionGroupCommand(
            SubscriptionManager subscriptionManager,
            DialogService dialogService
            )
        {
            SubscriptionManager = subscriptionManager;
            DialogService = dialogService;
        }

        public SubscriptionManager SubscriptionManager { get; }
        public DialogService DialogService { get; }

        protected override bool CanExecute(object parameter)
        {
            return true;
        }

        protected override async void Execute(object parameter)
        {
            var groupName = await DialogService.GetTextAsync("SubscriptionGroup_Create".ToCulturelizeString(), "", validater: (s) => !string.IsNullOrWhiteSpace(s));

            if (groupName == null) { return; }

            var subscription = new Models.Subscription.Subscription(Guid.NewGuid(), groupName);
            if (parameter is Models.Subscription.SubscriptionSource source)
            {
                subscription.Sources.Add(source);
            }

            SubscriptionManager.Subscriptions.Add(subscription);
        }
    }
}
