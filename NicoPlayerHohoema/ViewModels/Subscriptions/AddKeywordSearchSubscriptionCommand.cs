using NicoPlayerHohoema.Models.Subscriptions;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.ViewModels.Subscriptions
{
    public sealed class AddKeywordSearchSubscriptionCommand : DelegateCommandBase
    {
        private readonly SubscriptionManager _subscriptionManager;

        public AddKeywordSearchSubscriptionCommand(SubscriptionManager subscriptionManager)
        {
            _subscriptionManager = subscriptionManager;
        }
        protected override bool CanExecute(object parameter)
        {
            return parameter is string;
        }

        protected override void Execute(object parameter)
        {
            if (parameter is string keyword)
            {
                var subscription = _subscriptionManager.AddKeywordSearchSubscription(keyword);
                if (subscription != null)
                {
                    
                }
            }
        }
    }
}
