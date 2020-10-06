using Hohoema.Models.Domain.Subscriptions;
using Microsoft.AppCenter.Analytics;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Presentation.ViewModels.Subscriptions.Commands
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
                    Analytics.TrackEvent("Subscription_Added", new Dictionary<string, string>
                    {
                        { "SourceType", SubscriptionSourceType.SearchWithKeyword.ToString() }
                    });
                }
            }
        }
    }
}
