using Hohoema.Models.Domain.Playlist;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.UseCase.Playlist.Commands
{
    public sealed class PlaylistPlayAllCommand : DelegateCommandBase
    {
        private readonly HohoemaPlaylist _hohoemaPlaylist;

        public PlaylistPlayAllCommand(HohoemaPlaylist hohoemaPlaylist)
        {
            _hohoemaPlaylist = hohoemaPlaylist;
        }

        protected override bool CanExecute(object parameter)
        {
            return parameter is IPlaylist;
        }

        protected override void Execute(object parameter)
        {
            if (parameter is IPlaylist playlist)
            {
                _hohoemaPlaylist.Play(playlist);
            }
        }
    }
}
