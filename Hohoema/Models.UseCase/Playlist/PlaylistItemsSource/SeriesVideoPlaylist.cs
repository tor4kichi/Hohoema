using Hohoema.Models.Domain.Niconico.Series;
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Domain.Niconico.Video.Series;
using Hohoema.Models.Domain.Playlist;
using NiconicoToolkit.Series;
using NiconicoToolkit.Video;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Hohoema.Models.UseCase.Playlist.PlaylistItemsSource
{
    public sealed class SeriesVideoPlaylistItemsSourceFactory : IPlaylistItemsSourceFactory
    {
        private readonly SeriesProvider _seriesProvider;

        public SeriesVideoPlaylistItemsSourceFactory(SeriesProvider seriesProvider)
        {
            _seriesProvider = seriesProvider;
        }

        public async ValueTask<IPlaylist> Create(PlaylistId playlistId)
        {
            var result = await _seriesProvider.GetSeriesVideosAsync(playlistId.Id);
            return new SeriesVideoPlaylist(playlistId, result);
        }

        public IPlaylistSortOption DeserializeSortOptions(string serializedSortOptions)
        {
            return new SeriesPlaylistSortOption();
        }
    }


    
}

