
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Domain.Playlist;
using Hohoema.Models.UseCase.Playlist;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Presentation.ViewModels.Niconico.Video.Commands
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
