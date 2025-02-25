﻿#nullable enable
using Hohoema.Infra;
using Hohoema.Models.LocalMylist;
using Hohoema.Models.Playlist;
using Hohoema.Services.LocalMylist;
using System.Threading.Tasks;

namespace Hohoema.Services.Playlist.PlaylistFactory;

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
                throw new HohoemaException();
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
