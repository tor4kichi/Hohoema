using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Domain.Playlist;
using NiconicoToolkit.Series;
using NiconicoToolkit.Video;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Hohoema.Models.Domain.Niconico.Series
{

    public record SeriesPlaylistSortOption(SeriesVideoSortKey SortKey, PlaylistItemSortOrder SortOrder) : IPlaylistSortOption
    {
        public string Label => string.Empty;

        public bool Equals(IPlaylistSortOption other)
        {
            if (other is SeriesPlaylistSortOption seriesPlaylistSortOption)
            {
                return this == seriesPlaylistSortOption;
            }

            return false;
        }

        public string Serialize()
        {
            return JsonSerializer.Serialize(this);
        }

        public static SeriesPlaylistSortOption Deserialize(string json)
        {
            return JsonSerializer.Deserialize<SeriesPlaylistSortOption>(json);
        }
    }

    public enum SeriesVideoSortKey
    {
        AddedAt,
        PostedAt,
        Title,
        WatchCount,
        MylistCount,
        CommentCount,
    }

    public sealed class SeriesVideoPlaylist : ISortablePlaylist
    {
        public SeriesDetails SeriesDetails { get; }

        public SeriesVideoPlaylist(PlaylistId playlistId, SeriesDetails seriesDetails)
        {
            PlaylistId = playlistId;
            SeriesDetails = seriesDetails;
        }
        public int TotalCount => SeriesDetails.Series.Count ?? 0;

        public string Name => SeriesDetails.Series.Title;

        public PlaylistId PlaylistId { get; }

        public static IPlaylistSortOption[] SortOptions { get; } = new []
        {
            SeriesVideoSortKey.AddedAt,
            SeriesVideoSortKey.PostedAt,
            SeriesVideoSortKey.Title,
            SeriesVideoSortKey.WatchCount,
            SeriesVideoSortKey.MylistCount,
            SeriesVideoSortKey.CommentCount,
        }
        .SelectMany(x => 
        {
            return new[] { 
                    new SeriesPlaylistSortOption(x, PlaylistItemSortOrder.Desc) as IPlaylistSortOption,
                    new SeriesPlaylistSortOption(x, PlaylistItemSortOrder.Asc) as IPlaylistSortOption,
                };
        }).ToArray();


        IPlaylistSortOption[] IPlaylist.SortOptions => SortOptions;

        public static SeriesPlaylistSortOption DefaultSortOption { get; } = new SeriesPlaylistSortOption(SeriesVideoSortKey.AddedAt, PlaylistItemSortOrder.Asc);

        IPlaylistSortOption IPlaylist.DefaultSortOption => DefaultSortOption;

        public List<SeriesDetails.SeriesVideo> Videos => SeriesDetails.Videos;


        public Task<IEnumerable<IVideoContent>> GetAllItemsAsync(IPlaylistSortOption sortOption, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(SeriesDetails.Videos.Select(x => new SeriesVideoItem(x, SeriesDetails.Owner) as IVideoContent));
        }
    }



    public sealed class SeriesVideoItem : IVideoContent, IVideoContentProvider
    {
        private readonly SeriesDetails.SeriesVideo _video;
        private readonly SeriesDetails.SeriesOwner _owner;

        public SeriesVideoItem(SeriesDetails.SeriesVideo video, SeriesDetails.SeriesOwner owner)
        {
            _video = video;
            _owner = owner;
        }

        public string ProviderId => _owner.Id;

        public OwnerType ProviderType => _owner.OwnerType;

        public VideoId VideoId => _video.Id;

        public TimeSpan Length => _video.Duration;

        public string ThumbnailUrl => _video.ThumbnailUrl.OriginalString;

        public DateTime PostedAt => _video.PostAt;

        public string Title => _video.Title;

        public bool Equals(IVideoContent other)
        {
            return this.VideoId == other.VideoId;
        }
    }

}
