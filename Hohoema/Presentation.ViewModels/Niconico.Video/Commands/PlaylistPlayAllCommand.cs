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
            return parameter is IPlaylist;
        }

        protected override void Execute(object parameter)
        {
            if (parameter is IPlaylist playlist)
            {
                _messenger.Send(new VideoPlayRequestMessage() { PlaylistId = playlist.PlaylistId.Id, PlaylistOrigin = playlist.PlaylistId.Origin });
            }
        }
    }
}
