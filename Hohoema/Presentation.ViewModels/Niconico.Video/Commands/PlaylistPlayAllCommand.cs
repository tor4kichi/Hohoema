using Hohoema.Models.Domain.Playlist;
using Hohoema.Models.UseCase.Playlist;
using Microsoft.Toolkit.Mvvm.Messaging;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Presentation.ViewModels.Niconico.Video.Commands
{
    public sealed class PlaylistPlayAllCommand : DelegateCommandBase
    {
        private readonly IMessenger _messenger;

        public PlaylistPlayAllCommand(IMessenger messenger)
        {
            _messenger = messenger;
        }

        protected override bool CanExecute(object parameter)
        {
            if (parameter is IUserManagedPlaylist userManagedPlaylist)
            {
                return userManagedPlaylist.TotalCount > 0;
            }

            if (parameter is PlaylistToken playlistToken)
            {
                return true;
            }

            return parameter is IPlaylist;
        }

        protected override void Execute(object parameter)
        {
            if (parameter is IPlaylist playlist)
            {
                _messenger.Send(VideoPlayRequestMessage.PlayPlaylist(playlist));
            }
            else if (parameter is PlaylistToken playlistToken)
            {
                _messenger.Send(VideoPlayRequestMessage.PlayPlaylist(playlistToken));
            }
        }
    }
}
