#nullable enable
using CommunityToolkit.Mvvm.DependencyInjection;
using Hohoema.Contracts.Services;
using Hohoema.Models.Playlist;
using System.Text.Json;

namespace Hohoema.Models.Niconico.Search;

public record CeApiSearchVideoPlaylistSortOption(VideoSortKey SortKey, VideoSortOrder SortOrder) : IPlaylistSortOption
{
    private static string GetLocalizedLabel(VideoSortKey SortKey, VideoSortOrder SortOrder)
    {
        return Ioc.Default.GetRequiredService<ILocalizeService>().Translate($"VideoSortKey.{SortKey}_{SortOrder}");
    }

    public string Label { get; } = GetLocalizedLabel(SortKey, SortOrder);

    public bool Equals(IPlaylistSortOption other)
    {
        return other is CeApiSearchVideoPlaylistSortOption option && this == option;
    }

    public string Serialize()
    {
        return JsonSerializer.Serialize(this);
    }

    public static CeApiSearchVideoPlaylistSortOption Deserialize(string json)
    {
        return JsonSerializer.Deserialize<CeApiSearchVideoPlaylistSortOption>(json);
    }
}
