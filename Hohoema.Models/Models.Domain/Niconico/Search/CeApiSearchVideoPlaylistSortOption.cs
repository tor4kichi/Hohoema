using Hohoema.Models.Domain.Playlist;
using I18NPortable;
using NiconicoToolkit.SearchWithCeApi.Video;
using System.Text.Json;

namespace Hohoema.Models.Domain.Niconico.Search
{
    public record CeApiSearchVideoPlaylistSortOption(VideoSortKey SortKey, VideoSortOrder SortOrder) : IPlaylistSortOption
    {
        public string Label { get; } = $"VideoSortKey.{SortKey}_{SortOrder}".Translate();

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
}
