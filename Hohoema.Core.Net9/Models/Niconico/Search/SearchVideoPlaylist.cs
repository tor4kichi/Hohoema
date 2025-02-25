#nullable enable
using Hohoema.Models.Niconico.Video;
using Hohoema.Models.Playlist;
using Hohoema.Models.Video;
using CommunityToolkit.Diagnostics;
using NiconicoToolkit.Search;
using NiconicoToolkit.Search.Video;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Hohoema.Models.Niconico.Search;


public sealed class SearchVideoPlaylist : IUnlimitedPlaylist
{
    private readonly SearchClient _searchClient;

    public static SearchVideoPlaylistSortOption[] SortOptions { get; } = new SearchVideoPlaylistSortOption[]
    {
        new (SortKey.Hot, SortOrder.None),
        new (SortKey.Personalized, SortOrder.None),

        new (SortKey.RegisteredAt, SortOrder.Desc),
        new (SortKey.ViewCount, SortOrder.Desc),
        new (SortKey.CommentCount, SortOrder.Desc),
        new (SortKey.MylistCount, SortOrder.Desc),
        new (SortKey.LikeCount, SortOrder.Desc),
        new (SortKey.LastCommentTime, SortOrder.Desc),
        new (SortKey.Duration, SortOrder.Desc),

        new (SortKey.RegisteredAt, SortOrder.Asc),
        new (SortKey.ViewCount, SortOrder.Asc),
        new (SortKey.CommentCount, SortOrder.Asc),
        new (SortKey.MylistCount, SortOrder.Asc),
        new (SortKey.LikeCount, SortOrder.Asc),
        new (SortKey.LastCommentTime, SortOrder.Asc),
        new (SortKey.Duration, SortOrder.Asc),
    }        
    .ToArray();

    public static SearchVideoPlaylistSortOption DefaultSortOption => SortOptions[2]; // RegisteredAt Desc

    public SearchVideoPlaylist(PlaylistId playlistId, SearchClient searchClient)
    {
        PlaylistId = playlistId;
        _searchClient = searchClient;
    }
    public string Name => PlaylistId.Id;

    public PlaylistId PlaylistId { get; }

    IPlaylistSortOption[] IPlaylist.SortOptions => SortOptions;

    IPlaylistSortOption IPlaylist.DefaultSortOption => DefaultSortOption;


    public int OneTimeLoadItemsCount => 50;

    public async Task<IEnumerable<IVideoContent>> GetPagedItemsAsync(int pageIndex, int pageSize, IPlaylistSortOption sortOption, CancellationToken cancellationToken = default)
    {
        bool isTagSearch = PlaylistId.Origin switch
        {
            PlaylistItemsSourceOrigin.SearchWithKeyword => false,
            PlaylistItemsSourceOrigin.SearchWithTag => true,
            _ => throw new NotSupportedException(PlaylistId.Origin.ToString()),
        };

        SearchVideoPlaylistSortOption option = (sortOption as SearchVideoPlaylistSortOption)!;        
        var res = await _searchClient.Video.VideoSearchAsync(PlaylistId.Id, isTagSearch, pageIndex + 1, sortKey: option.SortKey, sortOrder: option.SortOrder);
        IVideoContent[] items = new IVideoContent[res.Data.Items.Length];
        int index = 0;
        foreach (var item in res.Data.Items)
        {
            items[index++] = new NvapiVideoContent(item);            
        }

        return items;
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
