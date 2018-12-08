using System.Collections.Generic;

namespace NicoPlayerHohoema.Models.Subscription
{
    public struct SubscriptionUpdateInfo
    {
        public Subscription Subscription { get; set; }
        public SubscriptionSource? Source { get; set; }
        public IEnumerable<Database.NicoVideo> FeedItems { get; set; }
        public IEnumerable<Database.NicoVideo> NewFeedItems { get; set; }

        public bool IsUpdateComplete => FeedItems != null;

        public bool IsFirstUpdate { get; set; }

    }

}
