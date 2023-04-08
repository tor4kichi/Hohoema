using Hohoema.Models.Subscriptions;

namespace Hohoema.ViewModels.Subscriptions;

public sealed class AddTagSearchSubscriptionCommand : CommandBase
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
                //Analytics.TrackEvent("Subscription_Added", new Dictionary<string, string>
                //{
                //    { "SourceType", SubscriptionSourceType.SearchWithTag.ToString() }
                //});
            }
        }
    }
}
