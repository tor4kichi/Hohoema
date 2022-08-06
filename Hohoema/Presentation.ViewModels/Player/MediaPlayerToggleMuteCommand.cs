using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Playback;

namespace Hohoema.Presentation.ViewModels.Player.Commands
{
    public sealed class MediaPlayerToggleMuteCommand : CommandBase
    {
        private readonly MediaPlayer _mediaPlayer;

        public MediaPlayerToggleMuteCommand(MediaPlayer mediaPlayer)
        {
            _mediaPlayer = mediaPlayer;
        }

        protected override bool CanExecute(object parameter)
        {
            return true;
        }

        protected override void Execute(object parameter)
        {
            _mediaPlayer.IsMuted = !_mediaPlayer.IsMuted;
        }
    }
}
