#nullable enable
using CommunityToolkit.Diagnostics;
using Hohoema.Contracts.Services;
using Hohoema.Models.Niconico.Video;
using Hohoema.Models.Playlist;
using Hohoema.Models.Subscriptions;
using System.Threading.Tasks;

namespace Hohoema.Services.Playlist.PlaylistFactory;

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
