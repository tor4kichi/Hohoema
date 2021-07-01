using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Domain.Playlist;
using Microsoft.Toolkit.Mvvm.Messaging;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Presentation.ViewModels.Niconico.Video.Commands
{
    public sealed class PlaylistPlayFromHereCommand : DelegateCommandBase
    {
        private readonly IPlaylist _playlist;
        private readonly IMessenger _messenger;

        public PlaylistPlayFromHereCommand(IPlaylist playlist, IMessenger messenger)
        {
            _playlist = playlist;
            _messenger = messenger;
        }

        protected override bool CanExecute(object parameter)
        {
            return parameter is IVideoContent;
        }

        protected override void Execute(object parameter)
        {
            if (parameter is IVideoContent video)
            {
                _messenger.Send(VideoPlayRequestMessage.PlayPlaylist(_playlist, video));
            }
        }
    }
}
