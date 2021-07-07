using Hohoema.Models.Domain.LocalMylist;
using Hohoema.Models.Domain.Playlist;
using Hohoema.Models.Infrastructure;
using Hohoema.Models.UseCase.Hohoema.LocalMylist;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Hohoema.Models.UseCase.Playlist.PlaylistItemsSource
{
    public sealed class LocalMylistPlaylistFactory : IPlaylistFactory
    {
        private readonly LocalMylistManager _localMylistManager;
        private readonly QueuePlaylist _queuePlaylist;

        public LocalMylistPlaylistFactory(
            LocalMylistManager localMylistManager,
            QueuePlaylist queuePlaylist
            )
        {
            _localMylistManager = localMylistManager;
            _queuePlaylist = queuePlaylist;
        }

        public ValueTask<IPlaylist> Create(PlaylistId playlistId)
        {
            if (playlistId == QueuePlaylist.Id)
            {
                return new(_queuePlaylist);
            }
            else
            {
                var localPlaylist = _localMylistManager.GetPlaylist(playlistId.Id);
                if (localPlaylist == null)
                {
                    throw new HohoemaExpception();
                }

                return new(localPlaylist);
            }
        }

        public IPlaylistSortOption DeserializeSortOptions(string serializedSortOptions)
        {
            if (string.IsNullOrEmpty(serializedSortOptions))
            {
                return new LocalPlaylistSortOption();
            }

            return LocalPlaylistSortOption.Deserialize(serializedSortOptions);
        }
    }

}
