﻿#nullable enable
using CommunityToolkit.Mvvm.DependencyInjection;
using Hohoema.Contracts.Services;
using Hohoema.Models.Niconico.Video;
using Hohoema.Models.Playlist;
using NiconicoToolkit.Community;
using NiconicoToolkit.User;
using NiconicoToolkit.Video;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Hohoema.Models.Niconico.Community;

public sealed class CommunityVideoPlaylist : IUnlimitedPlaylist
{
    private readonly CommunityId _communityId;
    private readonly CommunityProvider _communityProvider;

    public static CommunityVideoPlaylistSortOption[] SortOptions { get; } = new[]
    {
        CommunityVideoSortKey.RegisteredAt,
        CommunityVideoSortKey.FirstRetrieve,
        CommunityVideoSortKey.ViewCount,
        CommunityVideoSortKey.CommentCount,
        CommunityVideoSortKey.NewComment,
        CommunityVideoSortKey.MylistCount,
        CommunityVideoSortKey.Length,
    }
    .SelectMany(x => new CommunityVideoPlaylistSortOption[] { new(x, CommunityVideoSortOrder.Desc), new(x, CommunityVideoSortOrder.Asc) })
    .ToArray();

    public static CommunityVideoPlaylistSortOption DefaultSortOption => SortOptions[0];


    public CommunityVideoPlaylist(CommunityId communityId, PlaylistId playlistId, string communityName, CommunityProvider communityProvider)
    {
        _communityId = communityId;
        PlaylistId = playlistId;
        Name = communityName;
        _communityProvider = communityProvider;
    }

    public string Name { get; }

    public PlaylistId PlaylistId { get; }

    IPlaylistSortOption[] IPlaylist.SortOptions => SortOptions;

    IPlaylistSortOption IPlaylist.DefaultSortOption => DefaultSortOption;

    public int OneTimeLoadItemsCount => 30;

    public async Task<IEnumerable<IVideoContent>> GetPagedItemsAsync(int pageIndex, int pageSize, IPlaylistSortOption sortOption, CancellationToken cancellationToken = default)
    {
        CommunityVideoPlaylistSortOption sort = sortOption as CommunityVideoPlaylistSortOption;
        int head = pageIndex * pageSize;
        (CommunityVideoResponse, CommunityVideoListItemsResponse) items = await _communityProvider.GetCommunityVideoAsync(_communityId, head, pageSize, sort.SortKey, sort.SortOrder);
        return items.Item2.Data.Videos.Select(x => new CommunityVideoContent(x));
    }
}

public sealed class CommunityVideoContent : IVideoContent, IVideoContentProvider
{
    private readonly CommunityVideoListItemsResponse.CommunityVideoListItem _communityVideo;
    private readonly UserId _providerId;

    public CommunityVideoContent(CommunityVideoListItemsResponse.CommunityVideoListItem communityVideo)
    {
        _communityVideo = communityVideo;
        _providerId = _communityVideo.UserId;
        PostedAt = _communityVideo.GetCreateTime();
    }

    public VideoId VideoId => _communityVideo.Id;

    public TimeSpan Length => TimeSpan.FromSeconds(_communityVideo.ContentLength);

    public string ThumbnailUrl => _communityVideo.ThumbnailUrl.OriginalString;

    public DateTime PostedAt { get; }

    public string Title => _communityVideo.Title;

    public bool Equals(IVideoContent other)
    {
        return VideoId == other.VideoId;
    }

    public string ProviderId => _providerId;

    public OwnerType ProviderType => OwnerType.User;

}

public record CommunityVideoPlaylistSortOption(CommunityVideoSortKey SortKey, CommunityVideoSortOrder SortOrder) : IPlaylistSortOption
{
    private static string GetLocalizedLabel(CommunityVideoSortKey SortKey, CommunityVideoSortOrder SortOrder)
    {
        return Ioc.Default.GetRequiredService<ILocalizeService>().Translate($"CommunityVideoSortKey.{SortKey}_{SortOrder}");
    }

    public string Label { get; } = GetLocalizedLabel(SortKey, SortOrder);

    public bool Equals(IPlaylistSortOption other)
    {
        return other is CommunityVideoPlaylistSortOption option && this == option;
    }

    public string Serialize()
    {
        return JsonSerializer.Serialize(this);
    }

    public static CommunityVideoPlaylistSortOption Deserialize(string json)
    {
        return JsonSerializer.Deserialize<CommunityVideoPlaylistSortOption>(json);
    }
}
