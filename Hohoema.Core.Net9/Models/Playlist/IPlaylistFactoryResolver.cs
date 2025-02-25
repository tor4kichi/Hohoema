﻿#nullable enable
using System.Threading.Tasks;

namespace Hohoema.Models.Playlist;

public interface IPlaylistFactoryResolver
{
    IPlaylistFactory Resolve(PlaylistItemsSourceOrigin origin);
}

public interface IPlaylistFactory
{
    ValueTask<IPlaylist> Create(PlaylistId playlistId);
    IPlaylistSortOption DeserializeSortOptions(string serializedSortOptions);
}
