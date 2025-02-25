#nullable enable


using CommunityToolkit.Mvvm.DependencyInjection;
using Hohoema.Contracts.Services;
using Hohoema.Models.Niconico.Video;
using Hohoema.Models.Playlist;
using CommunityToolkit.Diagnostics;
using NiconicoToolkit.Mylist;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Hohoema.Models.Niconico.Mylist;

public record MylistPlaylistSortOption(MylistSortKey SortKey, MylistSortOrder SortOrder) : IPlaylistSortOption
{
    private static string GetLocalizedLabel(MylistSortKey SortKey, MylistSortOrder SortOrder)
    {
        return Ioc.Default.GetRequiredService<ILocalizeService>().Translate($"MylistSort.{SortKey}{SortOrder}");
    }

    private string _label;
    public string Label => _label ??= GetLocalizedLabel(SortKey, SortOrder);

    public string Serialize()
    {
        return JsonSerializer.Serialize(this);
    }

    public static MylistPlaylistSortOption Deserialize(string serializedText)
    {
        return string.IsNullOrEmpty(serializedText)
            ? new MylistPlaylistSortOption(MylistSortKey.RegisteredAt, MylistSortOrder.Desc)
            : JsonSerializer.Deserialize<MylistPlaylistSortOption>(serializedText);
    }

    public bool Equals(IPlaylistSortOption other)
    {
        return other is MylistPlaylistSortOption sortOption && this == sortOption;
    }
}

public class MylistPlaylist : IMylist, ISortablePlaylist
{

    public static MylistPlaylistSortOption[] SortOptions { get; } = new MylistPlaylistSortOption[]
    {
        new (MylistSortKey.AddedAt, MylistSortOrder.Desc),
        new (MylistSortKey.AddedAt, MylistSortOrder.Asc),
        new (MylistSortKey.Title, MylistSortOrder.Asc),
        new (MylistSortKey.Title, MylistSortOrder.Desc),
        new (MylistSortKey.MylistComment, MylistSortOrder.Asc),
        new (MylistSortKey.MylistComment, MylistSortOrder.Desc),
        new (MylistSortKey.RegisteredAt, MylistSortOrder.Desc),
        new (MylistSortKey.RegisteredAt, MylistSortOrder.Asc),
        new (MylistSortKey.ViewCount, MylistSortOrder.Desc),
        new (MylistSortKey.ViewCount, MylistSortOrder.Asc),
        new (MylistSortKey.LastCommentTime, MylistSortOrder.Desc),
        new (MylistSortKey.LastCommentTime, MylistSortOrder.Asc),
        new (MylistSortKey.CommentCount, MylistSortOrder.Desc),
        new (MylistSortKey.CommentCount, MylistSortOrder.Asc),
        new (MylistSortKey.MylistCount, MylistSortOrder.Desc),
        new (MylistSortKey.MylistCount, MylistSortOrder.Asc),
        new (MylistSortKey.Duration, MylistSortOrder.Desc),
        new (MylistSortKey.Duration, MylistSortOrder.Asc),

    };
    public static MylistPlaylistSortOption DefaultSortOption => SortOptions[1];

    IPlaylistSortOption[] IPlaylist.SortOptions => SortOptions;

    IPlaylistSortOption IPlaylist.DefaultSortOption => DefaultSortOption;

    int ISortablePlaylist.TotalCount => Count;

    private readonly MylistProvider _mylistProvider;

    public MylistPlaylist(MylistId id)
    {
        MylistId = id;
        PlaylistId = new PlaylistId() { Id = id, Origin = PlaylistItemsSourceOrigin.Mylist };
    }

    public MylistPlaylist(MylistId id, MylistProvider mylistProvider)
        : this(id)
    {
        _mylistProvider = mylistProvider;
    }

    public MylistId MylistId { get; }

    public string Name { get; internal set; }

    public int Count { get; internal set; }

    public int SortIndex { get; internal set; }

    public string Description { get; internal set; }

    public string UserId { get; internal set; }

    public bool IsPublic { get; internal set; }

    public MylistSortKey DefaultSortKey { get; internal set; }
    public MylistSortOrder DefaultSortOrder { get; internal set; }


    public Uri[] ThumbnailImages { get; internal set; }

    public Uri ThumbnailImage => ThumbnailImages?.FirstOrDefault();

    public DateTime CreateTime { get; internal set; }

    public PlaylistId PlaylistId { get; }

    public virtual async Task<MylistItemsGetResult> GetItemsAsync(int page, int pageSize, MylistSortKey sortKey, MylistSortOrder sortOrder)
    {
        Guard.IsTrue(IsPublic, nameof(IsPublic));

        // 他ユーザーマイリストとして取得を実行
        try
        {
            return await _mylistProvider.GetMylistVideoItems(MylistId, page, pageSize, sortKey, sortOrder);
        }
        catch
        {

        }

        return new MylistItemsGetResult() { IsSuccess = false };
    }

    private const int _pageSize = 100;

    public async Task<MylistItemsGetResult> GetAllItemsAsync(MylistSortKey sortKey = MylistSortKey.AddedAt, MylistSortOrder sortOrder = MylistSortOrder.Asc)
    {
        int page = 0;
        MylistItemsGetResult firstResult = await GetItemsAsync(page, _pageSize, sortKey, sortOrder);
        if (!firstResult.IsSuccess || firstResult.TotalCount == firstResult.Items.Count)
        {
            return firstResult;
        }

        page++;

        List<NicoVideo> nicovideoItemsList = new(firstResult.NicoVideoItems);
        List<MylistItem> itemsList = new(firstResult.Items);
        int totalCount = firstResult.TotalCount;
        int currentCount = firstResult.Items.Count;
        do
        {
            await Task.Delay(500);
            MylistItemsGetResult result = await GetItemsAsync(page, _pageSize, sortKey, sortOrder);
            if (result.IsSuccess)
            {
                itemsList.AddRange(result.Items);
                nicovideoItemsList.AddRange(result.NicoVideoItems);
            }

            page++;
            currentCount += result.Items.Count;
        }
        while (currentCount < totalCount);

        return new MylistItemsGetResult()
        {
            MylistId = MylistId,
            HeadPosition = 0,
            TotalCount = totalCount,
            IsSuccess = true,
            Items = new ReadOnlyCollection<MylistItem>(itemsList),
            NicoVideoItems = new ReadOnlyCollection<NicoVideo>(nicovideoItemsList)
        };
    }

    public int OneTimeItemsCount => _pageSize;

    public virtual async Task<IEnumerable<IVideoContent>> GetAllItemsAsync(IPlaylistSortOption sortOption, CancellationToken ct = default)
    {
        MylistPlaylistSortOption sort = sortOption as MylistPlaylistSortOption;
        MylistItemsGetResult result = await GetAllItemsAsync(sort.SortKey, sort.SortOrder);
        return result.NicoVideoItems;
    }
}
