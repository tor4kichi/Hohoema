#nullable enable
using CommunityToolkit.Mvvm.DependencyInjection;
using Hohoema.Contracts.Services;
using Hohoema.Models.Playlist;
using NiconicoToolkit.Search.Video;
using System.Text.Json;

namespace Hohoema.Models.Niconico.Search;

public record SearchVideoPlaylistSortOption(SortKey SortKey, SortOrder SortOrder) : IPlaylistSortOption
{
    public bool Equals(IPlaylistSortOption other)
    {
        return other is SearchVideoPlaylistSortOption option && this == option;
    }

    public string Serialize()
    {
        return JsonSerializer.Serialize(this);
    }

    public static SearchVideoPlaylistSortOption Deserialize(string json)
    {
        return JsonSerializer.Deserialize<SearchVideoPlaylistSortOption>(json);
    }
}
