using CommunityToolkit.Mvvm.DependencyInjection;
using Hohoema.Contracts.Services;
using Hohoema.Models.Playlist;
using NiconicoToolkit.SearchWithCeApi.Video;
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
        if (other is CeApiSearchVideoPlaylistSortOption option)
        {
            return this == option;
        }

        return false;
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
