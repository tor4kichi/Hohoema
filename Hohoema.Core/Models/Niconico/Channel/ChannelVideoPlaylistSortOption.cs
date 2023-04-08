using CommunityToolkit.Mvvm.DependencyInjection;
using Hohoema.Contracts.Services;
using Hohoema.Models.Playlist;
using NiconicoToolkit.Channels;
using System.Text.Json;

namespace Hohoema.Models.Niconico.Channel;

public record ChannelVideoPlaylistSortOption(ChannelVideoSortKey SortKey, ChannelVideoSortOrder SortOrder) : IPlaylistSortOption
{
    private static string GetLocalizedLabel(ChannelVideoSortKey SortKey, ChannelVideoSortOrder SortOrder)
    {
        return Ioc.Default.GetRequiredService<ILocalizeService>().Translate($"ChannelVideoSortKey.{SortKey}_{SortOrder}");
    }

    public string Label { get; } = GetLocalizedLabel(SortKey, SortOrder);


    public bool Equals(IPlaylistSortOption other)
    {
        return other is ChannelVideoPlaylistSortOption channelSortOption && this == channelSortOption;
    }

    public string Serialize()
    {
        return JsonSerializer.Serialize(this);
    }

    public static ChannelVideoPlaylistSortOption Deserialize(string json)
    {
        return JsonSerializer.Deserialize<ChannelVideoPlaylistSortOption>(json);
    }
}
