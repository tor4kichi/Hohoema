using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Domain.Playlist;
using I18NPortable;
using Microsoft.Toolkit.Diagnostics;
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
        public string Label { get; } = $"SeriesVideoSortKey.{SortKey}_{SortOrder}".Translate();

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

        public static SeriesPlaylistSortOption[] SortOptions { get; } = new []
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
                    new SeriesPlaylistSortOption(x, PlaylistItemSortOrder.Desc),
                    new SeriesPlaylistSortOption(x, PlaylistItemSortOrder.Asc),
                };
        }).ToArray();


        IPlaylistSortOption[] IPlaylist.SortOptions => SortOptions;

        public static SeriesPlaylistSortOption DefaultSortOption { get; } = new SeriesPlaylistSortOption(SeriesVideoSortKey.AddedAt, PlaylistItemSortOrder.Desc);

        IPlaylistSortOption IPlaylist.DefaultSortOption => DefaultSortOption;

        public List<SeriesDetails.SeriesVideo> Videos => SeriesDetails.Videos;

        static Comparison<SeriesDetails.SeriesVideo> GetComparision(SeriesVideoSortKey sortKey, PlaylistItemSortOrder sortOrder)
        {
            var isAsc = sortOrder == PlaylistItemSortOrder.Asc;
            return sortKey switch
            {
                SeriesVideoSortKey.AddedAt => null,
                SeriesVideoSortKey.PostedAt => isAsc ? (SeriesDetails.SeriesVideo x, SeriesDetails.SeriesVideo y) => DateTime.Compare(x.PostAt, y.PostAt) : (SeriesDetails.SeriesVideo x, SeriesDetails.SeriesVideo y) => DateTime.Compare(y.PostAt, x.PostAt),
                SeriesVideoSortKey.Title => isAsc ? (SeriesDetails.SeriesVideo x, SeriesDetails.SeriesVideo y) => string.Compare(x.Title, y.Title) : (SeriesDetails.SeriesVideo x, SeriesDetails.SeriesVideo y) => string.Compare(y.Title, x.Title),
                SeriesVideoSortKey.WatchCount => isAsc ? (SeriesDetails.SeriesVideo x, SeriesDetails.SeriesVideo y) => y.WatchCount - x.WatchCount : (SeriesDetails.SeriesVideo x, SeriesDetails.SeriesVideo y) => x.WatchCount - y.WatchCount,
                SeriesVideoSortKey.MylistCount => isAsc ? (SeriesDetails.SeriesVideo x, SeriesDetails.SeriesVideo y) => y.MylistCount - x.MylistCount : (SeriesDetails.SeriesVideo x, SeriesDetails.SeriesVideo y) => x.MylistCount - y.MylistCount,
                SeriesVideoSortKey.CommentCount => isAsc ? (SeriesDetails.SeriesVideo x, SeriesDetails.SeriesVideo y) => y.CommentCount - x.CommentCount : (SeriesDetails.SeriesVideo x, SeriesDetails.SeriesVideo y) => x.CommentCount - y.CommentCount,
                _ => throw new NotSupportedException(sortKey.ToString()),
            };
        }

        public List<SeriesDetails.SeriesVideo> GetSortedItems(SeriesPlaylistSortOption sortOption)
        {
            var list = SeriesDetails.Videos.ToList();
            if (GetComparision(sortOption.SortKey, sortOption.SortOrder) is not null and var sortComparision)
            {
                list.Sort(sortComparision);
            }
            else if (sortOption.SortKey == SeriesVideoSortKey.AddedAt)
            {
                if (sortOption.SortOrder == PlaylistItemSortOrder.Desc)
                {
                    list.Reverse();
                }
            }

            return list;
        }

        public Task<IEnumerable<IVideoContent>> GetAllItemsAsync(IPlaylistSortOption sortOption, CancellationToken cancellationToken = default)
        {
            Guard.IsOfType<SeriesPlaylistSortOption>(sortOption, nameof(sortOption));

            var list = GetSortedItems(sortOption as SeriesPlaylistSortOption);
            return Task.FromResult(list.Select(x => new SeriesVideoItem(x, SeriesDetails.Owner) as IVideoContent));
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
