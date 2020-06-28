using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Playback;

namespace Hohoema.UseCase.NicoVideoPlayer.Commands
{
    public sealed class MediaPlayerSeekCommand : DelegateCommandBase
    {
        private readonly MediaPlayer _mediaPlayer;

        public MediaPlayerSeekCommand(MediaPlayer mediaPlayer)
        {
            _mediaPlayer = mediaPlayer;
        }

        protected override bool CanExecute(object parameter)
        {
            return parameter is TimeSpan?;
        }

        protected override void Execute(object parameter)
        {
            if (parameter is TimeSpan timeDelta)
            {
                _mediaPlayer.PlaybackSession.Position += timeDelta;
            }
        }
    }
}
