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
        private readonly UseCase.NicoVideoPlayer.MediaPlayerSoundVolumeManager _soundVolumeManager;

        public MediaPlayerVolumeUpCommand(MediaPlayerSoundVolumeManager soundVolumeManager)
        {
            _soundVolumeManager = soundVolumeManager;
        }

        protected override bool CanExecute(object parameter)
        {
            return parameter is double;
        }

        protected override void Execute(object parameter)
        {
            if (parameter is double val)
            {
                _soundVolumeManager.Volume = _soundVolumeManager.Volume + val;
            }
        }
    }

    public sealed class MediaPlayerVolumeDownCommand : DelegateCommandBase
    {
        private readonly UseCase.NicoVideoPlayer.MediaPlayerSoundVolumeManager _soundVolumeManager;

        public MediaPlayerVolumeDownCommand(MediaPlayerSoundVolumeManager soundVolumeManager)
        {
            _soundVolumeManager = soundVolumeManager;
        }

        protected override bool CanExecute(object parameter)
        {
            return parameter is double;
        }

        protected override void Execute(object parameter)
        {
            if (parameter is double val)
            {
                _soundVolumeManager.Volume = _soundVolumeManager.Volume - val;
            }
        }
    }
}
