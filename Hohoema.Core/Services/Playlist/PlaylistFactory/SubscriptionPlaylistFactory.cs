#nullable enable
using CommunityToolkit.Diagnostics;
using Hohoema.Contracts.Services;
using Hohoema.Contracts.Subscriptions;
using Hohoema.Models.Niconico.Video;
using Hohoema.Models.Playlist;
using Hohoema.Models.Subscriptions;
using System.Threading.Tasks;

namespace Hohoema.Services.Playlist.PlaylistFactory;

public sealed class SubscriptionPlaylistFactory : IPlaylistFactory
{
    private readonly SubscriptionManager _subscriptionManager;
    private readonly NicoVideoProvider _nicoVideoProvider;    

    public SubscriptionPlaylistFactory(
        SubscriptionManager subscriptionManager,
        NicoVideoProvider nicoVideoProvider     
        )
    {
        _subscriptionManager = subscriptionManager;
        _nicoVideoProvider = nicoVideoProvider;        
    }

    public ValueTask<IPlaylist> Create(PlaylistId playlistId)
    {
        Guard.IsTrue(playlistId.Origin is PlaylistItemsSourceOrigin.Subscription);
        var subscription = _subscriptionManager.GetSubscription(SubscriptionId.Parse(playlistId.Id));
        return new(new SubscriptionPlaylist(subscription, _subscriptionManager, _nicoVideoProvider));
    }

    public IPlaylistSortOption DeserializeSortOptions(string serializedSortOptions)
    {
        return SubscriptionGroupPlaylist.SortOptions[0];
    }
}