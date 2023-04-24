#nullable enable
using Hohoema.Contracts.Services;
using Hohoema.Contracts.Subscriptions;
using Hohoema.Models.Niconico.Video;
using Hohoema.Models.Playlist;
using LiteDB;
using NiconicoToolkit.Video;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Hohoema.Models.Subscriptions;
public sealed class SubscriptionGroupPlaylist 
    : IUserManagedPlaylist
    , IPlaylistItemWatchedAware
{
    private readonly SubscriptionGroup? _group;
    private readonly SubscriptionManager _subscriptionManager;
    private readonly NicoVideoProvider _nicoVideoProvider;
    private readonly ILocalizeService _localizeService;

    public SubscriptionGroupPlaylist(
        SubscriptionGroup? group,
        SubscriptionManager subscriptionManager,
        NicoVideoProvider nicoVideoProvider,
        ILocalizeService localizeService
        )
    {
        _group = group;
        _subscriptionManager = subscriptionManager;
        _nicoVideoProvider = nicoVideoProvider;
        _localizeService = localizeService;
    }

    public string Name => _group?.Name ?? _localizeService.Translate("All");

    public PlaylistId PlaylistId => new PlaylistId(PlaylistItemsSourceOrigin.SubscriptionGroup , (_group?.GroupId.ToString() ?? _subscriptionManager.AllSubscriptouGroupId));

    public static readonly SubscriptionSortOption[] SortOptions = new[] { new SubscriptionSortOption() };

    IPlaylistSortOption[] IPlaylist.SortOptions { get; } = SubscriptionGroupPlaylist.SortOptions;

    IPlaylistSortOption IPlaylist.DefaultSortOption => SortOptions[0];

    public static readonly SubscriptionSortOption DefaultSortOption = SubscriptionGroupPlaylist.SortOptions[0];

    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    public int TotalCount => _group is not null ? _subscriptionManager.GetFeedVideosCountWithNewer(_group.GroupId) : _subscriptionManager.GetFeedVideosCountWithNewer();

    public async Task<IEnumerable<IVideoContent>> GetAllItemsAsync(IPlaylistSortOption sortOption, CancellationToken cancellationToken = default)
    {        
        List<IVideoContent> videos = new ();
        _subscriptionIdVideoIdMap.Clear();
        foreach (var subscVideo in _subscriptionManager.GetSubscFeedVideosNewerAt(_group?.GroupId).OrderBy(x => x.PostAt))
        {
            if (TryAddMapAndIsAlreadyContainItem(subscVideo))
            {
                continue;
            }
            var nicoVideo = await _nicoVideoProvider.GetCachedVideoInfoAsync(subscVideo.VideoId, cancellationToken);
            videos.Add(nicoVideo);            
        }
        return videos;
    }

    bool TryAddMapAndIsAlreadyContainItem(SubscFeedVideo video)
    {
        bool isAlreadyContainItem = true;
        if (_subscriptionIdVideoIdMap.TryGetValue(video.VideoId, out var list) is false)
        {
            _subscriptionIdVideoIdMap.Add(video.VideoId, list = new List<SubscriptionId>());
            isAlreadyContainItem = false;
        }
        list.Add(video.SourceSubscId);
        return isAlreadyContainItem;
    }


    
    private readonly Dictionary<VideoId, List<SubscriptionId>> _subscriptionIdVideoIdMap = new ();
    

    void IPlaylistItemWatchedAware.OnVideoWatched(IVideoContent video)
    {
        if (_subscriptionIdVideoIdMap.TryGetValue(video.VideoId, out var list))
        {
            foreach (var subscriptionId in list)
            {
                _subscriptionManager.UpdateSubscriptionCheckedAt(subscriptionId, video.PostedAt);
            }
        }
        else if (_group != null)
        {
            _subscriptionManager.SetSubscriptionCheckedAt(_group.GroupId, video);
        }
    }
}

public sealed class SubscriptionSortOption : IPlaylistSortOption
{
    public string Label { get; } = "SubscriptionGroupSort_PostAt_Asc";

    public SubscriptionSortOption()
    {

    }

    public bool Equals(IPlaylistSortOption other)
    {
        return true;
    }

    public string Serialize()
    {
        return string.Empty;
    }
}
