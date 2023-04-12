#nullable enable
using Hohoema.Models.Niconico.Video;
using Hohoema.Models.Playlist;
using Microsoft.Toolkit.Diagnostics;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Hohoema.Models.Niconico.Search;


public enum VideoSortOrder
{
    [Description("a")]
    Asc,
    [Description("d")]
    Desc
}

public enum VideoSortKey
{
    [Description("n")]
    NewComment,
    [Description("v")]
    ViewCount,
    [Description("m")]
    MylistCount,
    [Description("r")]
    CommentCount,
    [Description("f")]
    FirstRetrieve,
    [Description("l")]
    Length,
    [Description("h")]
    Popurarity,
    [Description("c")]
    MylistPopurarity,
    [Description("n")]
    Relation,
    [Description("i")]
    VideoCount
}

public sealed class CeApiSearchVideoPlaylist : IUnlimitedPlaylist
{
    private readonly SearchProvider _searchProvider;

    public static CeApiSearchVideoPlaylistSortOption[] SortOptions { get; } = new[]
    {
        VideoSortKey.FirstRetrieve,
        VideoSortKey.ViewCount,
        VideoSortKey.NewComment,
        VideoSortKey.MylistCount,
        VideoSortKey.CommentCount,
        VideoSortKey.Length,
    }
    .SelectMany(x => new CeApiSearchVideoPlaylistSortOption[] { new(x, VideoSortOrder.Desc), new(x, VideoSortOrder.Asc) })
        .ToArray();

    public static CeApiSearchVideoPlaylistSortOption DefaultSortOption => SortOptions[0];

    public CeApiSearchVideoPlaylist(PlaylistId playlistId, SearchProvider searchProvider)
    {
        PlaylistId = playlistId;
        _searchProvider = searchProvider;
    }
    public string Name => PlaylistId.Id;

    public PlaylistId PlaylistId { get; }

    IPlaylistSortOption[] IPlaylist.SortOptions => SortOptions;

    IPlaylistSortOption IPlaylist.DefaultSortOption => DefaultSortOption;


    public int OneTimeLoadItemsCount => 50;

    public async Task<IEnumerable<IVideoContent>> GetPagedItemsAsync(int pageIndex, int pageSize, IPlaylistSortOption sortOption, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("動画検索は現在工事中です。");
        //CeApiSearchVideoPlaylistSortOption sort = sortOption as CeApiSearchVideoPlaylistSortOption;
        //int head = pageIndex * pageSize;

        //VideoListingResponse result = PlaylistId.Origin switch
        //{
        //    PlaylistItemsSourceOrigin.SearchWithKeyword => await _searchProvider.GetKeywordSearch(PlaylistId.Id, head, pageSize, sort.SortKey, sort.SortOrder),
        //    PlaylistItemsSourceOrigin.SearchWithTag => await _searchProvider.GetTagSearch(PlaylistId.Id, head, pageSize, sort.SortKey, sort.SortOrder),
        //    _ => throw new NotSupportedException(),
        //};

        //Guard.IsTrue(result.IsOK, nameof(result.IsOK));

        //return result.Videos.Select(x => new CeApiSearchVideoContent(x.Video));
    }
}
