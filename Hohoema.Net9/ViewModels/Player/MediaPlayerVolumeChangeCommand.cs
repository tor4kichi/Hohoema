#nullable enable
using Hohoema.Models.Player;

namespace Hohoema.ViewModels.Player.Commands;

public sealed partial class MediaPlayerVolumeUpCommand : CommandBase
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

public sealed partial class MediaPlayerVolumeDownCommand : CommandBase
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
