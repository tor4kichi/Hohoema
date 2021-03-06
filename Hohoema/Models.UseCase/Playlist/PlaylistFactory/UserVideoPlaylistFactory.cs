﻿using Hohoema.Models.Domain.Niconico.User;
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Domain.Playlist;
using Hohoema.Models.Domain.User;
using NiconicoToolkit.User;
using NiconicoToolkit.Video;
using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Hohoema.Models.UseCase.Playlist.PlaylistFactory
{
    public sealed class UserVideoPlaylistFactory : IPlaylistFactory
    {
        private readonly UserProvider _userProvider;

        public UserVideoPlaylistFactory(UserProvider userProvider)
        {
            _userProvider = userProvider;
        }

        public async ValueTask<IPlaylist> Create(PlaylistId playlistId)
        {
            UserId userId = playlistId.Id;
            var info = await _userProvider.GetUserInfoAsync(userId);
            return new UserVideoPlaylist(userId, playlistId, info.ScreenName, _userProvider);
        }

        public IPlaylistSortOption DeserializeSortOptions(string serializedSortOptions)
        {
            return UserVideoPlaylistSortOption.Deserialize(serializedSortOptions);
        }
    }


}
