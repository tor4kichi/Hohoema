#nullable enable
using Hohoema.Models.Niconico.Video;
using Hohoema.Models.Playlist;
using CommunityToolkit.Diagnostics;
using NiconicoToolkit.Channels;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Hohoema.Models.Niconico.Channel;

public sealed class ChannelVideoPlaylist : IUnlimitedPlaylist
{
    private readonly ChannelId _channelId;
    private readonly ChannelProvider _channelProvider;

    public ChannelVideoPlaylist(ChannelId channelId, PlaylistId playlistId, string name, ChannelProvider channelProvider)
    {
        _channelId = channelId;
        PlaylistId = playlistId;
        Name = name;
        _channelProvider = channelProvider;
    }
    public string Name { get; }

    public PlaylistId PlaylistId { get; }

    public static ChannelVideoPlaylistSortOption[] SortOptions { get; } = new[]
    {
        ChannelVideoSortKey.FirstRetrieve,
        ChannelVideoSortKey.ViewCount,
        ChannelVideoSortKey.CommentCount,
        ChannelVideoSortKey.NewComment,
        ChannelVideoSortKey.MylistCount,
        ChannelVideoSortKey.Length,
    }
    .SelectMany(x =>
    {
        return new[] { new ChannelVideoPlaylistSortOption(x, ChannelVideoSortOrder.Desc), new ChannelVideoPlaylistSortOption(x, ChannelVideoSortOrder.Asc) };
    })
        .ToArray();


    IPlaylistSortOption[] IPlaylist.SortOptions => ChannelVideoPlaylist.SortOptions;

    IPlaylistSortOption IPlaylist.DefaultSortOption => ChannelVideoPlaylist.DefaultSortOption;

    public static ChannelVideoPlaylistSortOption DefaultSortOption => SortOptions[0];

    public int OneTimeLoadItemsCount => ChannelClient.OneTimeItemsCountOnGetChannelVideoAsync;

    public async Task<IEnumerable<IVideoContent>> GetPagedItemsAsync(int pageIndex, int pageSize, IPlaylistSortOption sortOption, CancellationToken cancellationToken = default)
    {
        ChannelVideoPlaylistSortOption sort = sortOption as ChannelVideoPlaylistSortOption;
        ChannelVideoResponse items = await _channelProvider.GetChannelVideo(_channelId, pageIndex, sort.SortKey, sort.SortOrder);

        Guard.IsTrue(items.IsSuccess, nameof(items.IsSuccess));

        return items.Data.Videos.Select(x => new ChannelVideoContent(x, _channelId));
    }
}
