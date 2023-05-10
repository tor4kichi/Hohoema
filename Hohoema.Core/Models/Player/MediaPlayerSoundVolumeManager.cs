#nullable enable
using CommunityToolkit.Mvvm.ComponentModel;
using Reactive.Bindings.Extensions;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using Windows.Media.Playback;

namespace Hohoema.Models.Player;

public class MediaPlayerSoundVolumeManager : ObservableObject
{
    
    public MediaPlayerSoundVolumeManager(
        PlayerSettings playerSettings,
        MediaPlayer mediaPlayer
        )
    {
        _playerSettings = playerSettings;
        _mediaPlayer = mediaPlayer;

        Volume = _playerSettings.SoundVolume;
        IsLoudnessCorrectionEnabled = _playerSettings.IsLoudnessCorrectionEnabled;

        _ = new[]
        {
            this.ObserveProperty(x => x.IsLoudnessCorrectionEnabled).ToUnit(),
            this.ObserveProperty(x => x.Volume).ToUnit(),
            this.ObserveProperty(x => x.LoudnessCorrectionValue).ToUnit()
        }
        .Merge()
        .Subscribe(_ =>
        {
            _mediaPlayer.Volume = IsLoudnessCorrectionEnabled
            ? Volume * LoudnessCorrectionValue
            : Volume
            ;

            Debug.WriteLine($"LoudnessCorrection: {IsLoudnessCorrectionEnabled} , Volume: {Volume} , LoudnessCorrectionValue: {LoudnessCorrectionValue} , MediaPlayer.Volume: {_mediaPlayer.Volume}");
        });

    }
    private readonly PlayerSettings _playerSettings;
    private readonly MediaPlayer _mediaPlayer;
    private double _Volume;

    
    public double Volume
    {
        get => _Volume;
        set
        {
            if (SetProperty(ref _Volume, Math.Clamp(value, 0.0, 1.0)))
            {
                _playerSettings.SoundVolume = _Volume;
            }
        }
    }

    private double _LoudnessCorrectionValue;
    public double LoudnessCorrectionValue
    {
        get => _LoudnessCorrectionValue;
        set => SetProperty(ref _LoudnessCorrectionValue, Math.Clamp(value, 0.1, 1.0));
    }

    private bool _isLoudnessCorrectionEnabled;

    
    public bool IsLoudnessCorrectionEnabled
    {
        get => _isLoudnessCorrectionEnabled;
        set
        {
            if (SetProperty(ref _isLoudnessCorrectionEnabled, value))
            {
                _playerSettings.IsLoudnessCorrectionEnabled = value;
            }
        }
    }
}
