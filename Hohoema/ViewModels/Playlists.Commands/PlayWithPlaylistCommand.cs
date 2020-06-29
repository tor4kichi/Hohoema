using Hohoema.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.UseCase.Playlist.Commands
{
    public sealed class PlayWithPlaylistCommand : VideoContentSelectionCommandBase
    {
        private readonly IPlaylist _playlist;
        private readonly HohoemaPlaylist _hohoemaPlaylist;

        public PlayWithPlaylistCommand(IPlaylist playlist, HohoemaPlaylist hohoemaPlaylist)
        {
            _playlist = playlist;
            _hohoemaPlaylist = hohoemaPlaylist;
        }

        protected override void Execute(IVideoContent content)
        {
            _hohoemaPlaylist.Play(content, _playlist);
        }
    }
}
