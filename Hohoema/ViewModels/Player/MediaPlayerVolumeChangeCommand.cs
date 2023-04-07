using Hohoema.Models.Player;
using Hohoema.Services.Niconico.Player;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Playback;

namespace Hohoema.ViewModels.Player.Commands
{
    public sealed class MediaPlayerVolumeUpCommand : CommandBase
    {
        private readonly MediaPlayerSoundVolumeManager _soundVolumeManager;

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

    public sealed class MediaPlayerVolumeDownCommand : CommandBase
    {
        private readonly MediaPlayerSoundVolumeManager _soundVolumeManager;

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
