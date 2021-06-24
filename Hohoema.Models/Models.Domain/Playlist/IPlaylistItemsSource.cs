using Hohoema.Models.Domain.Niconico.Video;
using Microsoft.Toolkit.Collections;
using Microsoft.Toolkit.Diagnostics;
using NiconicoToolkit.Video;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Hohoema.Models.Domain.Playlist
{
    public record PlaylistId
    {
        public PlaylistItemsSourceOrigin Origin { get; init; }
        public string Id { get; init; }
        public string? SortOptions { get; init; }
    }


    public record  PlaylistItem
    {
        public PlaylistItem(PlaylistId playlistId, int itemIndex, VideoId itemId)
        {
            Guard.IsNotNull(playlistId, nameof(playlistId));
            Guard.IsGreaterThanOrEqualTo(itemIndex, 0, nameof(itemIndex));
            Guard.IsNotNullOrWhiteSpace(itemId, nameof(itemId));

            PlaylistId = playlistId;
            ItemIndex = itemIndex;
            ItemId = itemId;
        }
        public PlaylistId PlaylistId { get; init; }
        public int ItemIndex { get; internal set; }
        public VideoId ItemId { get; init; }
    }

    public interface IPlaylistItemsSource : IPlaylist
    {
        public int OneTimeItemsCount { get; }
        ValueTask<IEnumerable<PlaylistItem>> GetRangeAsync(int start, int count, CancellationToken ct = default);
        
    }
    public interface IShufflePlaylistItemsSource : IPlaylistItemsSource, INotifyCollectionChanged, INotifyPropertyChanged
    {
        public int MaxItemsCount { get; }
    }

}
