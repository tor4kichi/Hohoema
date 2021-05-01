using Hohoema.Models.Domain.Niconico.Channel;
using Hohoema.Models.Domain.Niconico.User;
using Hohoema.Models.Domain.Playlist;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.UseCase.NicoVideos
{
    public sealed class PlaylistResolver
    {
        private readonly MylistRepository _mylistRepository;
        private readonly LocalMylistManager _localMylistManager;
        private readonly ChannelProvider _channelProvider;
        private readonly UserProvider _userProvider;

        public PlaylistResolver(
            MylistRepository mylistRepository,
            LocalMylistManager localMylistManager,
            ChannelProvider channelProvider,
            UserProvider userProvider
            )
        {
            _mylistRepository = mylistRepository;
            _localMylistManager = localMylistManager;
            _channelProvider = channelProvider;
            _userProvider = userProvider;
        }

        public async ValueTask<IPlaylist> ResolvePlaylistAsync(PlaylistOrigin origin, string playlistId)
        {
            return origin switch
            {
                PlaylistOrigin.Mylist => await _mylistRepository.GetMylist(playlistId),
                PlaylistOrigin.Local => _localMylistManager.GetPlaylist(playlistId),
                PlaylistOrigin.ChannelVideos => throw new NotImplementedException(),
                PlaylistOrigin.UserVideos => throw new NotImplementedException(),
            };
        }
    }
}
