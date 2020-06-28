namespace Hohoema.Models.Subscription
{
    public struct SubscriptionDestination
    {
        public string Label { get; }
        public string PlaylistId { get; }
        public SubscriptionDestinationTarget Target { get; }

        public SubscriptionDestination(string label, SubscriptionDestinationTarget target, string playlistId)
        {
            PlaylistId = playlistId;
            Target = target;
            Label = label;
        }

    }

}
