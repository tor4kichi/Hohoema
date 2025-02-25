#nullable enable
using Hohoema.Models.Subscriptions;

namespace Hohoema.ViewModels.Subscriptions;

public sealed partial class AddKeywordSearchSubscriptionCommand : CommandBase
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
                //Analytics.TrackEvent("Subscription_Added", new Dictionary<string, string>
                //{
                //    { "SourceType", SubscriptionSourceType.SearchWithKeyword.ToString() }
                //});
            }
        }
    }
}
