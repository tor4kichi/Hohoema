using NicoPlayerHohoema.Models.Subscriptions;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.ViewModels.Subscriptions
{
    public sealed class AddTagSearchSubscriptionCommand : DelegateCommandBase
    {
        private readonly SubscriptionManager _subscriptionManager;

        public AddTagSearchSubscriptionCommand(SubscriptionManager subscriptionManager)
        {
            _subscriptionManager = subscriptionManager;
        }
        protected override bool CanExecute(object parameter)
        {
            return parameter is string;
        }

        protected override void Execute(object parameter)
        {
            if (parameter is string tag)
            {
                var subscription = _subscriptionManager.AddTagSearchSubscription(tag);
                if (subscription != null)
                {

                }
            }
        }
    }
}
