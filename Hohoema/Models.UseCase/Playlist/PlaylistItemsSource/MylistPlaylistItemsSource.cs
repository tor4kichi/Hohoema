using Hohoema.Models.Domain.Niconico.Mylist;
using Hohoema.Models.Domain.Playlist;
using NiconicoToolkit.Mylist;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Hohoema.Models.UseCase.Playlist.PlaylistItemsSource
{
    public sealed class MylistPlaylistItemsSourceFactory : IPlaylistItemsSourceFactory
    {
        private readonly LoginUserOwnedMylistManager _loginUserOwnedMylistManager;
        private readonly MylistResolver _mylistResolver;

        public MylistPlaylistItemsSourceFactory(LoginUserOwnedMylistManager loginUserOwnedMylistManager,
            MylistResolver mylistResolver
            )
        {
            _loginUserOwnedMylistManager = loginUserOwnedMylistManager;
            _mylistResolver = mylistResolver;
        }

        public async ValueTask<IPlaylist> Create(PlaylistId playlistId)
        {
            return await _mylistResolver.GetMylistAsync(playlistId.Id);
        }

        public IPlaylistSortOptions DeserializeSortOptions(string serializedSortOptions)
        {
            return MylistPlaylistSortOptions.Deserialize(serializedSortOptions);
        }
    }
}
