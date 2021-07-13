using Hohoema.Models.Domain.Playlist;
using I18NPortable;
using NiconicoToolkit.Channels;
using System.Text.Json;

namespace Hohoema.Models.Domain.Niconico.Channel
{
    public record ChannelVideoPlaylistSortOption(ChannelVideoSortKey SortKey, ChannelVideoSortOrder SortOrder) : IPlaylistSortOption
    {
        public string Label { get; } = $"ChannelVideoSortKey.{SortKey}_{SortOrder}".Translate();

        
        public bool Equals(IPlaylistSortOption other)
        {
            if (other is ChannelVideoPlaylistSortOption channelSortOption)
            {
                return this == channelSortOption;
            }

            return false;
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
}
