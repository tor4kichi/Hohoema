using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.ViewModels
{
    public class SubscriptionControlViewModel : BindableBase
    {
        public SubscriptionControlViewModel(
            Models.Subscription.SubscriptionManager subscriptionManager,
            Views.Subscriptions.ChoiceSubscriptionSourceCommand choiceSubscriptionSourceCommand,
            Views.Subscriptions.MultiSelectSubscriptionDestinationCommand multiSelectSubscriptionDestinationCommand,
            Views.Subscriptions.OpenSubscriptionSourceCommand openSubscriptionSourceCommand,
            Views.Subscriptions.OpenSubscriptionDestinationCommand openSubscriptionDestinationCommand
            )
        {
            SubscriptionManager = subscriptionManager;
            ChoiceSubscriptionSourceCommand = choiceSubscriptionSourceCommand;
            MultiSelectSubscriptionDestinationCommand = multiSelectSubscriptionDestinationCommand;
            OpenSubscriptionSourceCommand = openSubscriptionSourceCommand;
            OpenSubscriptionDestinationCommand = openSubscriptionDestinationCommand;
        }

        public Models.Subscription.SubscriptionManager SubscriptionManager { get; }
        public Views.Subscriptions.ChoiceSubscriptionSourceCommand ChoiceSubscriptionSourceCommand { get; }
        public Views.Subscriptions.MultiSelectSubscriptionDestinationCommand MultiSelectSubscriptionDestinationCommand { get; }
        public Views.Subscriptions.OpenSubscriptionSourceCommand OpenSubscriptionSourceCommand { get; }
        public Views.Subscriptions.OpenSubscriptionDestinationCommand OpenSubscriptionDestinationCommand { get; }
    }
}
