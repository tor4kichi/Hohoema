﻿using Hohoema.Models.Domain.Niconico.Community;
using Hohoema.Models.Domain.Playlist;
using Microsoft.Toolkit.Diagnostics;
using NiconicoToolkit.Community;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.UseCase.Playlist.PlaylistFactory
{
    public sealed class CommunityVideoPlaylistFactory : IPlaylistFactory
    {
        private readonly CommunityProvider _communityProvider;

        public CommunityVideoPlaylistFactory(CommunityProvider communityProvider)
        {
            _communityProvider = communityProvider;
        }

        public async ValueTask<IPlaylist> Create(PlaylistId playlistId)
        {
            CommunityId communityId = playlistId.Id;
            var communityInfo = await _communityProvider.GetCommunityInfo(communityId);
            Guard.IsTrue(communityInfo.IsOK, nameof(communityInfo.IsOK));
            return new CommunityVideoPlaylist(communityId, playlistId, communityInfo.Community.Name, _communityProvider);
        }

        public IPlaylistSortOption DeserializeSortOptions(string serializedSortOptions)
        {
            return CommunityVideoPlaylistSortOption.Deserialize(serializedSortOptions);
        }
    }
}
