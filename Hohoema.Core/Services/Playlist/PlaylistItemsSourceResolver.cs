#nullable enable
using Hohoema.Models.Playlist;
using Hohoema.Models.Subscriptions;
using Hohoema.Services.Playlist.PlaylistFactory;
using System;

namespace Hohoema.Services.Playlist;


public sealed class PlaylistItemsSourceResolver : IPlaylistFactoryResolver
{
    private readonly Lazy<LocalMylistPlaylistFactory> _localMylistPlaylistFactory;
    private readonly Lazy<MylistPlaylistFactory> _mylistPlaylistFactory;
    private readonly Lazy<SeriesVideoPlaylistFactory> _seriesVideoPlaylistFactory;
    private readonly Lazy<ChannelVideoPlaylistFactory> _channelVideoPlaylistFactory;
    private readonly Lazy<UserVideoPlaylistFactory> _userVideoPlaylistFactory;
    private readonly Lazy<SubscriptionGroupPlaylistFactory> _subscriptionGroupPlaylistFactory;
    private readonly Lazy<SubscriptionPlaylistFactory> _subscriptionPlaylistFactory;

    public PlaylistItemsSourceResolver(
        Lazy<LocalMylistPlaylistFactory> localMylistPlaylistFactory,
        Lazy<MylistPlaylistFactory> mylistPlaylistFactory,
        Lazy<SeriesVideoPlaylistFactory> seriesVideoPlaylistFactory,
        Lazy<ChannelVideoPlaylistFactory> channelVideoPlaylistFactory,
        Lazy<UserVideoPlaylistFactory> userVideoPlaylistFactory,
        Lazy<SubscriptionGroupPlaylistFactory> subscriptionGroupPlaylistFactory,
        Lazy<SubscriptionPlaylistFactory> subscriptionPlaylistFactory
        )
    {
        _localMylistPlaylistFactory = localMylistPlaylistFactory;
        _mylistPlaylistFactory = mylistPlaylistFactory;
        _seriesVideoPlaylistFactory = seriesVideoPlaylistFactory;
        _channelVideoPlaylistFactory = channelVideoPlaylistFactory;
        _userVideoPlaylistFactory = userVideoPlaylistFactory;        
        _subscriptionGroupPlaylistFactory = subscriptionGroupPlaylistFactory;
        _subscriptionPlaylistFactory = subscriptionPlaylistFactory;
    }

    public IPlaylistFactory Resolve(PlaylistItemsSourceOrigin origin)
    {
        return origin switch
        {
            PlaylistItemsSourceOrigin.Local => _localMylistPlaylistFactory.Value,
            PlaylistItemsSourceOrigin.Mylist => _mylistPlaylistFactory.Value,
            PlaylistItemsSourceOrigin.Series => _seriesVideoPlaylistFactory.Value,
            PlaylistItemsSourceOrigin.ChannelVideos => _channelVideoPlaylistFactory.Value,
            PlaylistItemsSourceOrigin.UserVideos => _userVideoPlaylistFactory.Value,            
            PlaylistItemsSourceOrigin.Subscription => _subscriptionPlaylistFactory.Value,
            PlaylistItemsSourceOrigin.SubscriptionGroup => _subscriptionGroupPlaylistFactory.Value,
            _ => throw new NotSupportedException($"Not supported to playlist play for {origin}"),
        };
    }
}
