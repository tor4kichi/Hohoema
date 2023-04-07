using Hohoema.Models.Niconico.Series;
using Hohoema.Models.Niconico.Video;
using Hohoema.Models.Niconico.Video.Series;
using Hohoema.Models.Playlist;
using NiconicoToolkit.Series;
using NiconicoToolkit.Video;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Hohoema.Models.UseCase.Playlist.PlaylistFactory
{
    public sealed class SeriesVideoPlaylistFactory : IPlaylistFactory
    {
        private readonly SeriesProvider _seriesProvider;

        public SeriesVideoPlaylistFactory(SeriesProvider seriesProvider)
        {
            _seriesProvider = seriesProvider;
        }

        public async ValueTask<IPlaylist> Create(PlaylistId playlistId)
        {
            var result = await _seriesProvider.GetSeriesVideosAsync(playlistId.Id, 0, 25);
            return new SeriesVideoPlaylist(playlistId, result, _seriesProvider);
        }

        public IPlaylistSortOption DeserializeSortOptions(string serializedSortOptions)
        {
            return SeriesPlaylistSortOption.Deserialize(serializedSortOptions);
        }
    }
}

