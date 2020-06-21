using NicoPlayerHohoema.Models.Subscriptions;
using NicoPlayerHohoema.UseCase.Playlist;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.UseCase.Subscriptions
{
    public sealed class FeedResultAddToWatchLater
    {
        private readonly SubscriptionManager _subscriptionManager;
        private readonly HohoemaPlaylist _hohoemaPlaylist;

        public FeedResultAddToWatchLater(
            SubscriptionManager subscriptionManager,
            HohoemaPlaylist hohoemaPlaylist
            )
        {
            _subscriptionManager = subscriptionManager;
            _hohoemaPlaylist = hohoemaPlaylist;
            _subscriptionManager.Updated += _subscriptionManager_Updated;
        }

        private void _subscriptionManager_Updated(object sender, SubscriptionFeedUpdateResult e)
        {
            if (e.IsSuccessed && (e.NewVideos?.Any() ?? false))
            {
                foreach (var newVideo in e.NewVideos)
                {
                    if (!Database.VideoPlayedHistoryDb.IsVideoPlayed(newVideo.Id))
                    {
                        _hohoemaPlaylist.AddWatchAfterPlaylist(newVideo);

                        Debug.WriteLine("[FeedResultAddToWatchLater] added: " + newVideo.Label);
                    }
                }
            }
        }
    }
}
