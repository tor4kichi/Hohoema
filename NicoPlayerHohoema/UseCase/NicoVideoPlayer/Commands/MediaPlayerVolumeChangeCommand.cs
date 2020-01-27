using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Playback;

namespace NicoPlayerHohoema.UseCase.NicoVideoPlayer.Commands
{
    public sealed class MediaPlayerVolumeUpCommand : DelegateCommandBase
    {
        private readonly MediaPlayer _mediaPlayer;

        public MediaPlayerVolumeUpCommand(MediaPlayer mediaPlayer)
        {
            _mediaPlayer = mediaPlayer;
        }

        protected override bool CanExecute(object parameter)
        {
            return parameter is double;
        }

        protected override void Execute(object parameter)
        {
            if (parameter is double val)
            {
                _mediaPlayer.Volume = Math.Clamp(_mediaPlayer.Volume + val, 0.0, 1.0);
            }
        }
    }

    public sealed class MediaPlayerVolumeDownCommand : DelegateCommandBase
    {
        private readonly MediaPlayer _mediaPlayer;

        public MediaPlayerVolumeDownCommand(MediaPlayer mediaPlayer)
        {
            _mediaPlayer = mediaPlayer;
        }

        protected override bool CanExecute(object parameter)
        {
            return parameter is double;
        }

        protected override void Execute(object parameter)
        {
            if (parameter is double val)
            {
                _mediaPlayer.Volume = Math.Clamp(_mediaPlayer.Volume - val, 0.0, 1.0);
            }
        }
    }
}
