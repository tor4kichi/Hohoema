﻿#nullable enable
using Hohoema.Models.Niconico.Mylist;
using Hohoema.Models.Playlist;
using System.Threading.Tasks;

namespace Hohoema.Services.Playlist.PlaylistFactory;

public sealed class MylistPlaylistFactory : IPlaylistFactory
{
    private readonly MylistResolver _mylistResolver;

    public MylistPlaylistFactory(
        MylistResolver mylistResolver
        )
    {
        _mylistResolver = mylistResolver;
    }

    public async ValueTask<IPlaylist> Create(PlaylistId playlistId)
    {
        return await _mylistResolver.GetMylistAsync(playlistId.Id);
    }

    public IPlaylistSortOption DeserializeSortOptions(string serializedSortOptions)
    {
        return MylistPlaylistSortOption.Deserialize(serializedSortOptions);
    }
}
