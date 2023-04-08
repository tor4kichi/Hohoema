using CommunityToolkit.Mvvm.DependencyInjection;
using Hohoema.Contracts.Services;
using Hohoema.Models.Niconico.Video;
using Hohoema.Models.Niconico.Video.Series;
using Hohoema.Models.Playlist;
using Microsoft.Toolkit.Diagnostics;
using NiconicoToolkit.Series;
using NiconicoToolkit.Video;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace Hohoema.Models.Niconico.Series;


public record SeriesPlaylistSortOption(SeriesVideoSortKey SortKey, PlaylistItemSortOrder SortOrder) : IPlaylistSortOption
{
    private static string GetLocalizedLabel(SeriesVideoSortKey SortKey, PlaylistItemSortOrder SortOrder)
    {
        return Ioc.Default.GetRequiredService<ILocalizeService>().Translate($"SeriesVideoSortKey.{SortKey}_{SortOrder}");
    }

    public string Label { get; } = GetLocalizedLabel(SortKey, SortOrder);

    public bool Equals(IPlaylistSortOption other)
    {
        if (other is SeriesPlaylistSortOption seriesPlaylistSortOption)
        {
            return this == seriesPlaylistSortOption;
        }

        return false;
    }

    public string Serialize()
    {
        return JsonSerializer.Serialize(this);
    }

    public static SeriesPlaylistSortOption Deserialize(string json)
    {
        return JsonSerializer.Deserialize<SeriesPlaylistSortOption>(json);
    }
}

public enum SeriesVideoSortKey
{
    AddedAt,
    PostedAt,
    Title,
    WatchCount,
    MylistCount,
    CommentCount,
}

public sealed class SeriesVideoPlaylist : ISortablePlaylist
{
    private readonly SeriesProvider _seriesProvider;

    public NvapiSeriesVidoesResponseContainer SeriesDetails { get; }

    public SeriesVideoPlaylist(PlaylistId playlistId, NvapiSeriesVidoesResponseContainer seriesDetails, SeriesProvider seriesProvider)
    {
        PlaylistId = playlistId;
        _seriesProvider = seriesProvider;
        SeriesDetails = seriesDetails;
    }
    public int TotalCount => SeriesDetails.Data.TotalCount ?? 0;

    public string Name => SeriesDetails.Data.Detail.Title;

    public PlaylistId PlaylistId { get; }

    public static SeriesPlaylistSortOption[] SortOptions { get; } = new []
    {
        SeriesVideoSortKey.AddedAt,
        SeriesVideoSortKey.PostedAt,
        SeriesVideoSortKey.Title,
        SeriesVideoSortKey.WatchCount,
        SeriesVideoSortKey.MylistCount,
        SeriesVideoSortKey.CommentCount,
    }
    .SelectMany(x => 
    {
        return new[] { 
                new SeriesPlaylistSortOption(x, PlaylistItemSortOrder.Desc),
                new SeriesPlaylistSortOption(x, PlaylistItemSortOrder.Asc),
            };
    }).ToArray();


    IPlaylistSortOption[] IPlaylist.SortOptions => SortOptions;

    public static SeriesPlaylistSortOption DefaultSortOption { get; } = new SeriesPlaylistSortOption(SeriesVideoSortKey.AddedAt, PlaylistItemSortOrder.Asc);

    IPlaylistSortOption IPlaylist.DefaultSortOption => DefaultSortOption;

    public List<NiconicoToolkit.Series.SeriesVideoItem> Videos => SeriesDetails.Data.Items;

    //int IUnlimitedPlaylist.OneTimeLoadItemsCount => 100;

    static Comparison<NiconicoToolkit.Series.SeriesVideoItem> GetComparision(SeriesVideoSortKey sortKey, PlaylistItemSortOrder sortOrder)
    {
        var isAsc = sortOrder == PlaylistItemSortOrder.Asc;
        return sortKey switch
        {
            SeriesVideoSortKey.AddedAt => null,
            SeriesVideoSortKey.PostedAt => isAsc ? (NiconicoToolkit.Series.SeriesVideoItem x, NiconicoToolkit.Series.SeriesVideoItem y) => DateTimeOffset.Compare(x.Video.RegisteredAt, y.Video.RegisteredAt) : (NiconicoToolkit.Series.SeriesVideoItem x, NiconicoToolkit.Series.SeriesVideoItem y) => DateTimeOffset.Compare(y.Video.RegisteredAt, x.Video.RegisteredAt),
            SeriesVideoSortKey.Title => isAsc ? (NiconicoToolkit.Series.SeriesVideoItem x, NiconicoToolkit.Series.SeriesVideoItem y) => string.Compare(x.Video.Title, y.Video.Title) : (NiconicoToolkit.Series.SeriesVideoItem x, NiconicoToolkit.Series.SeriesVideoItem y) => string.Compare(y.Video.Title, x.Video.Title),
            SeriesVideoSortKey.WatchCount => isAsc ? (NiconicoToolkit.Series.SeriesVideoItem x, NiconicoToolkit.Series.SeriesVideoItem y) => x.Video.Count.View - y.Video.Count.View : (NiconicoToolkit.Series.SeriesVideoItem x, NiconicoToolkit.Series.SeriesVideoItem y) => y.Video.Count.View - x.Video.Count.View,
            SeriesVideoSortKey.MylistCount => isAsc ? (NiconicoToolkit.Series.SeriesVideoItem x, NiconicoToolkit.Series.SeriesVideoItem y) => x.Video.Count.Mylist - y.Video.Count.Mylist : (NiconicoToolkit.Series.SeriesVideoItem x, NiconicoToolkit.Series.SeriesVideoItem y) => y.Video.Count.Mylist - x.Video.Count.Mylist,
            SeriesVideoSortKey.CommentCount => isAsc ? (NiconicoToolkit.Series.SeriesVideoItem x, NiconicoToolkit.Series.SeriesVideoItem y) => x.Video.Count.Comment - y.Video.Count.Comment : (NiconicoToolkit.Series.SeriesVideoItem x, NiconicoToolkit.Series.SeriesVideoItem y) => y.Video.Count.Comment - x.Video.Count.Comment,
            _ => throw new NotSupportedException(sortKey.ToString()),
        };
    }

    private  async Task<IEnumerable<NiconicoToolkit.Series.SeriesVideoItem>> GetItems(int page, int pageSize)
    {
        int head = page * pageSize;
        int tail = page * pageSize + pageSize;
        if (head < SeriesDetails.Data.Items.Count)
        {
            List<NiconicoToolkit.Series.SeriesVideoItem> items = new();
            items.AddRange(SeriesDetails.Data.Items.Skip(head).Take(tail));
            if (tail > SeriesDetails.Data.Items.Count)
            {
                var remainCount = tail - SeriesDetails.Data.Items.Count;
                var result = await _seriesProvider.GetSeriesVideosAsync(PlaylistId.Id, page, pageSize);
                items.AddRange(result.Data.Items.Skip(head).Take(tail));
            }
            return items;
        }
        else
        {
            var result = await _seriesProvider.GetSeriesVideosAsync(PlaylistId.Id, page, pageSize);
            return result.Data.Items;
        }
    }

    public static List<NiconicoToolkit.Series.SeriesVideoItem> GetSortedItems(List<NiconicoToolkit.Series.SeriesVideoItem> videoItems, SeriesPlaylistSortOption sortOption)
    {
        var list = videoItems;
        if (GetComparision(sortOption.SortKey, sortOption.SortOrder) is not null and var sortComparision)
        {
            list.Sort(sortComparision);
        }
        else if (sortOption.SortKey == SeriesVideoSortKey.AddedAt)
        {
            if (sortOption.SortOrder == PlaylistItemSortOrder.Desc)
            {
                list.Reverse();
            }
        }

        return list;
    }

    public async Task<IEnumerable<IVideoContent>> GetAllItemsAsync(IPlaylistSortOption sortOption, CancellationToken cancellationToken = default)
    {
        Guard.IsOfType<SeriesPlaylistSortOption>(sortOption, nameof(sortOption));

        List<NiconicoToolkit.Series.SeriesVideoItem> items = new();
        int page = 0;
        while (items.Count < TotalCount)
        {
            var result = await _seriesProvider.GetSeriesVideosAsync(PlaylistId.Id, page, 100);
            items.AddRange(result.Data.Items);
            page++;
        }
        return GetSortedItems(items, sortOption as SeriesPlaylistSortOption).Select(x => new SeriesVideoItem(x));
    }
}



public sealed class SeriesVideoItem : IVideoContent, IVideoContentProvider
{
    private readonly NiconicoToolkit.Series.SeriesVideoItem _video;        

    public SeriesVideoItem(NiconicoToolkit.Series.SeriesVideoItem video)
    {
        _video = video;            
    }

    public string ProviderId => _video.Video.Owner.Id;

    public OwnerType ProviderType => _video.Video.Owner.OwnerType;

    public VideoId VideoId => _video.Video.Id;

    public TimeSpan Length => TimeSpan.FromSeconds(_video.Video.Duration);

    public string ThumbnailUrl => _video.Video.Thumbnail.ListingUrl?.OriginalString ?? _video.Video.Thumbnail.MiddleUrl?.OriginalString;

    public DateTime PostedAt => _video.Video.RegisteredAt.DateTime;

    public string Title => _video.Video.Title;

    public bool Equals(IVideoContent other)
    {
        return this.VideoId == other.VideoId;
    }

    public int ViewCount => _video.Video.Count.View;
    public int MylistCount => _video.Video.Count.Mylist;
    public int CommentCount => _video.Video.Count.Comment;
    public int LikeCount => _video.Video.Count.Like;

    public string ProviderName => _video.Video.Owner.Name;

    public bool RequireSensitiveMasking => _video.Video.RequireSensitiveMasking;

    public bool IsDeleted => _video.Video.IsDeleted;
}
