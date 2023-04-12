#nullable enable
using CommunityToolkit.Mvvm.DependencyInjection;
using Hohoema.Contracts.Services;
using Hohoema.Models.Playlist;
using NiconicoToolkit.User;
using System.Text.Json;

namespace Hohoema.Models.User;

public record UserVideoPlaylistSortOption(UserVideoSortKey SortKey, UserVideoSortOrder SortOrder) : IPlaylistSortOption
{
    private static string GetLocalizedLabel(UserVideoSortKey SortKey, UserVideoSortOrder SortOrder)
    {
        return Ioc.Default.GetRequiredService<ILocalizeService>().Translate($"UserVideoSortKey.{SortKey}_{SortOrder}");
    }

    public string Label { get; } = GetLocalizedLabel(SortKey, SortOrder);

    public bool Equals(IPlaylistSortOption other)
    {
        return other is UserVideoPlaylistSortOption option && this == option;
    }

    public string Serialize()
    {
        return JsonSerializer.Serialize(this);
    }

    public static UserVideoPlaylistSortOption Deserialize(string json)
    {
        return JsonSerializer.Deserialize<UserVideoPlaylistSortOption>(json);
    }
}
