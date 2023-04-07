using Hohoema.Models.LocalMylist;
using Hohoema.Models.Niconico.Mylist;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Playlist
{
    public static class PlaylistExtension
    {
        public static PlaylistItemsSourceOrigin GetOrigin(this IPlaylist playlist)
        {
            switch (playlist)
            {
                case QueuePlaylist:
                    return PlaylistItemsSourceOrigin.Local;
                case LocalPlaylist:
                    return PlaylistItemsSourceOrigin.Local;
                case MylistPlaylist:
                    return PlaylistItemsSourceOrigin.Mylist;
                default:
                    throw new NotSupportedException(playlist?.GetType().Name);
            }
        }

        public static bool IsQueuePlaylist(this IPlaylist localPlaylist)
        {
            return localPlaylist?.PlaylistId == QueuePlaylist.Id;
        }

        public static bool IsUniquePlaylist(this IPlaylist playlist)
        {
            return IsQueuePlaylist(playlist);
        }
    }
}
