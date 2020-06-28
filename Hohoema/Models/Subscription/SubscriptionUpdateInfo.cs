using Hohoema.Interfaces;
using System.Collections.Generic;

namespace Hohoema.Models.Subscription
{
    public struct SubscriptionUpdateInfo
    {
        public Subscription Subscription { get; set; }
        public SubscriptionSource? Source { get; set; }
        public IEnumerable<IVideoContent> FeedItems { get; set; }
        public IEnumerable<IVideoContent> NewFeedItems { get; set; }

        public bool IsUpdateComplete => FeedItems != null;

        public bool IsFirstUpdate { get; set; }

    }

}
