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
    private readonly Lazy<CommunityVideoPlaylistFactory> _communityVideoPlaylistFactory;
    private readonly Lazy<SearchPlaylistFactory> _searchPlaylistFactory;
    private readonly Lazy<SubscriptionGroupPlaylistFactory> _subscriptionGroupPlaylistFactory;

    public PlaylistItemsSourceResolver(
        Lazy<LocalMylistPlaylistFactory> localMylistPlaylistFactory,
        Lazy<MylistPlaylistFactory> mylistPlaylistFactory,
        Lazy<SeriesVideoPlaylistFactory> seriesVideoPlaylistFactory,
        Lazy<ChannelVideoPlaylistFactory> channelVideoPlaylistFactory,
        Lazy<UserVideoPlaylistFactory> userVideoPlaylistFactory,
        Lazy<CommunityVideoPlaylistFactory> communityVideoPlaylistFactory,
        Lazy<SearchPlaylistFactory> searchPlaylistFactory,
        Lazy<SubscriptionGroupPlaylistFactory> subscriptionGroupPlaylistFactory
        )
    {
        _localMylistPlaylistFactory = localMylistPlaylistFactory;
        _mylistPlaylistFactory = mylistPlaylistFactory;
        _seriesVideoPlaylistFactory = seriesVideoPlaylistFactory;
        _channelVideoPlaylistFactory = channelVideoPlaylistFactory;
        _userVideoPlaylistFactory = userVideoPlaylistFactory;
        _communityVideoPlaylistFactory = communityVideoPlaylistFactory;
        _searchPlaylistFactory = searchPlaylistFactory;
        _subscriptionGroupPlaylistFactory = subscriptionGroupPlaylistFactory;
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
            PlaylistItemsSourceOrigin.CommunityVideos => _communityVideoPlaylistFactory.Value,
            PlaylistItemsSourceOrigin.SearchWithKeyword => _searchPlaylistFactory.Value,
            PlaylistItemsSourceOrigin.SearchWithTag => _searchPlaylistFactory.Value,
            PlaylistItemsSourceOrigin.SubscriptionGroup => _subscriptionGroupPlaylistFactory.Value,
            _ => throw new NotSupportedException(origin.ToString()),
        };
    }
}
