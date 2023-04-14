#nullable enable
using CommunityToolkit.Diagnostics;
using Hohoema.Contracts.Services;
using Hohoema.Contracts.Subscriptions;
using Hohoema.Models.Niconico.Video;
using Hohoema.Models.Playlist;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Hohoema.Models.Subscriptions;
public sealed class SubscriptionGroupPlaylist : IUserManagedPlaylist
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

    public PlaylistId PlaylistId => new PlaylistId(PlaylistItemsSourceOrigin.SubscriptionGroup , _group.GroupId.ToString());

    public static readonly SubscriptionGroupSortOption[] SortOptions = new[] { new SubscriptionGroupSortOption() };

    IPlaylistSortOption[] IPlaylist.SortOptions { get; } = SubscriptionGroupPlaylist.SortOptions;

    IPlaylistSortOption IPlaylist.DefaultSortOption => SortOptions[0];

    public static readonly SubscriptionGroupSortOption DefaultSortOption = SubscriptionGroupPlaylist.SortOptions[0];

    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    public int TotalCount => _subscriptionManager.GetSubscriptionGroupVideosCount(_group?.GroupId);

    public async Task<IEnumerable<IVideoContent>> GetAllItemsAsync(IPlaylistSortOption sortOption, CancellationToken cancellationToken = default)
    {
        List<IVideoContent> videos = new ();
        foreach (var subscVideo in _subscriptionManager.GetSubscFeedVideosRaw(_group?.GroupId).OrderBy(x => x.PostAt))
        {
            var nicoVideo = await _nicoVideoProvider.GetCachedVideoInfoAsync(subscVideo.VideoId, cancellationToken);
            videos.Add(nicoVideo);
        }
        return videos;
    }
}

public sealed class SubscriptionGroupSortOption : IPlaylistSortOption
{
    public string Label { get; } = "SubscriptionGroupSort_PostAt_Asc";

    public SubscriptionGroupSortOption()
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

public sealed class SubscriptionGroupPlaylistFactory : IPlaylistFactory
{
    private readonly SubscriptionManager _subscriptionManager;
    private readonly NicoVideoProvider _nicoVideoProvider;
    private readonly ILocalizeService _localizeService;

    public SubscriptionGroupPlaylistFactory(
        SubscriptionManager subscriptionManager,
        NicoVideoProvider nicoVideoProvider,
        ILocalizeService localizeService
        )
    {
        _subscriptionManager = subscriptionManager;
        _nicoVideoProvider = nicoVideoProvider;
        _localizeService = localizeService;
    }

    public ValueTask<IPlaylist> Create(PlaylistId playlistId)
    {
        Guard.IsTrue(playlistId.Origin is PlaylistItemsSourceOrigin.SubscriptionGroup);
        var group = _subscriptionManager.GetSubscriptionGroup(playlistId.Id);
        return new(new SubscriptionGroupPlaylist(group, _subscriptionManager, _nicoVideoProvider, _localizeService));
    }

    public IPlaylistSortOption DeserializeSortOptions(string serializedSortOptions)
    {
        return SubscriptionGroupPlaylist.SortOptions[0];
    }
}
