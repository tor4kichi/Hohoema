#nullable enable
using Hohoema.Models.LocalMylist;
using Hohoema.Models.Niconico.Mylist;
using System;

namespace Hohoema.Models.Playlist;

public static class PlaylistExtension
{
    public static PlaylistItemsSourceOrigin GetOrigin(this IPlaylist playlist)
    {
        return playlist switch
        {
            QueuePlaylist => PlaylistItemsSourceOrigin.Local,
            LocalPlaylist => PlaylistItemsSourceOrigin.Local,
            MylistPlaylist => PlaylistItemsSourceOrigin.Mylist,
            _ => throw new NotSupportedException(playlist?.GetType().Name),
        };
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
