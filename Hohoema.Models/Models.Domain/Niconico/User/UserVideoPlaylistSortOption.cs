using Hohoema.Models.Domain.Playlist;
using I18NPortable;
using NiconicoToolkit.User;
using System.Text.Json;

namespace Hohoema.Models.Domain.User
{
    public record UserVideoPlaylistSortOption(UserVideoSortKey SortKey, UserVideoSortOrder SortOrder) : IPlaylistSortOption
    {
        public string Label { get; } = $"UserVideoSortKey.{SortKey}_{SortOrder}".Translate();

        public bool Equals(IPlaylistSortOption other)
        {
            if (other is UserVideoPlaylistSortOption option)
            {
                return this == option;
            }

            return false;
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
}
