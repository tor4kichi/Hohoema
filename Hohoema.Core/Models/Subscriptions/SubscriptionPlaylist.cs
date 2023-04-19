#nullable enable
using Hohoema.Contracts.Services;
using Hohoema.Models.Niconico.Video;
using Hohoema.Models.Playlist;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Hohoema.Models.Subscriptions;
public sealed  class SubscriptionPlaylist
    : IUserManagedPlaylist
    , IPlaylistItemWatchedAware
{
    private readonly Subscription _subscription;
    private readonly SubscriptionManager _subscriptionManager;
    private readonly NicoVideoProvider _nicoVideoProvider;

    public SubscriptionPlaylist(
        Subscription subscription,
        SubscriptionManager subscriptionManager,
        NicoVideoProvider nicoVideoProvider
        )
    {
        _subscription = subscription;
        _subscriptionManager = subscriptionManager;
        _nicoVideoProvider = nicoVideoProvider;        
    }


    public string Name => _subscription.Label;

    public PlaylistId PlaylistId => new PlaylistId(PlaylistItemsSourceOrigin.Subscription, _subscription.SubscriptionId.ToString());

    public static readonly SubscriptionSortOption[] SortOptions = new[] { new SubscriptionSortOption() };

    IPlaylistSortOption[] IPlaylist.SortOptions { get; } = SubscriptionGroupPlaylist.SortOptions;

    IPlaylistSortOption IPlaylist.DefaultSortOption => SortOptions[0];

    public static readonly SubscriptionSortOption DefaultSortOption = SubscriptionGroupPlaylist.SortOptions[0];

    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    public int TotalCount => _subscriptionManager.GetFeedVideosCountWithNewer(_subscription);

    public async Task<IEnumerable<IVideoContent>> GetAllItemsAsync(IPlaylistSortOption sortOption, CancellationToken cancellationToken = default)
    {
        List<IVideoContent> videos = new();
        foreach (var subscVideo in _subscriptionManager.GetSubscFeedVideosNewerAt(_subscription).OrderBy(x => x.PostAt))
        {
            var nicoVideo = await _nicoVideoProvider.GetCachedVideoInfoAsync(subscVideo.VideoId, cancellationToken);
            videos.Add(nicoVideo);
        }
        return videos;
    }

    void IPlaylistItemWatchedAware.OnVideoWatched(IVideoContent video)
    {
        //_subscriptionManager.SetCheckedAt(GroupId, video.PostedAt);
    }
}
