using Hohoema.Models.Repository;
using Hohoema.UseCase.Playlist;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.ViewModels.Player.Commands
{
    public sealed class PlayVideoCommand : DelegateCommandBase
    {
        private readonly HohoemaPlaylist _hohoemaPlaylist;

        public PlayVideoCommand(HohoemaPlaylist hohoemaPlaylist)
        {
            _hohoemaPlaylist = hohoemaPlaylist;
        }
        protected override bool CanExecute(object parameter)
        {
            return parameter is string
                || parameter is IVideoContent
                ;
        }

        protected override void Execute(object parameter)
        {
            if (parameter is string contentId)
            {
                _hohoemaPlaylist.Play(contentId);
            }
            else if (parameter is IVideoContent playlistItem)
            {
                _hohoemaPlaylist.Play(playlistItem);
            }
        }
    }
}
