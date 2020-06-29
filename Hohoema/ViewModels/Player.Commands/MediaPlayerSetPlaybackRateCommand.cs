using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Playback;

namespace Hohoema.ViewModels.Player.Commands
{
    public sealed class MediaPlayerSetPlaybackRateCommand : DelegateCommandBase
    {
        private readonly MediaPlayer _mediaPlayer;

        public MediaPlayerSetPlaybackRateCommand(MediaPlayer mediaPlayer)
        {
            _mediaPlayer = mediaPlayer;
        }
        protected override bool CanExecute(object parameter)
        {
            return parameter is double;
        }

        const double MinPlaybackRate = 1.0 / 60.0;
        const double MaxPlaybackRate = 2.0;


        protected override void Execute(object parameter)
        {
            if (parameter is double val)
            {
                _mediaPlayer.PlaybackSession.PlaybackRate = Math.Clamp(val, MinPlaybackRate, MaxPlaybackRate);
            }
        }
    }
}
