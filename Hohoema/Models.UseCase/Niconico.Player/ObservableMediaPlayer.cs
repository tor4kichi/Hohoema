using Microsoft.Toolkit.Mvvm.ComponentModel;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Playback;

namespace Hohoema.Models.UseCase.Niconico.Player
{
    public sealed class ObservableMediaPlayer : ObservableObject, IDisposable
    {
        private readonly MediaPlayer _mediaPlayer;
        private readonly IScheduler _scheduler;
        CompositeDisposable _disposables = new CompositeDisposable();

        public ObservableMediaPlayer(MediaPlayer mediaPlayer, IScheduler scheduler)
        {
            _mediaPlayer = mediaPlayer;
            _scheduler = scheduler;
            CurrentState = WindowsObservable.FromEventPattern<MediaPlaybackSession, object>(
                h => _mediaPlayer.PlaybackSession.PlaybackStateChanged += h,
                h => _mediaPlayer.PlaybackSession.PlaybackStateChanged -= h
                )
                .Select(x => x.Sender.PlaybackState)
                .ToReadOnlyReactiveProperty(eventScheduler: _scheduler)
                .AddTo(_disposables);
            NowBuffering = CurrentState.Select(x => x == MediaPlaybackState.Buffering || x == MediaPlaybackState.Opening)
                .ToReadOnlyReactiveProperty(eventScheduler: _scheduler)
                .AddTo(_disposables);
            NowPlaying = CurrentState
                .Select(x =>
                {
                    return
                        //						x == MediaPlaybackState.Opening ||
                        x == MediaPlaybackState.Buffering ||
                        x == MediaPlaybackState.Playing;
                })
                .ToReactiveProperty(_scheduler)
                .AddTo(_disposables);

            PlaybackRate = WindowsObservable.FromEventPattern<MediaPlaybackSession, object>(
                h => _mediaPlayer.PlaybackSession.PlaybackRateChanged += h,
                h => _mediaPlayer.PlaybackSession.PlaybackRateChanged -= h
                )
                .Select(x => x.Sender.PlaybackRate)
                .ToReadOnlyReactiveProperty(initialValue: _mediaPlayer.PlaybackSession.PlaybackRate, eventScheduler: _scheduler)
                .AddTo(_disposables);


            IsMuted = WindowsObservable.FromEventPattern<MediaPlayer, object>(
                h => _mediaPlayer.IsMutedChanged += h,
                h => _mediaPlayer.IsMutedChanged -= h
                )
                .Select(x => x.Sender.IsMuted)
                .ToReadOnlyReactiveProperty(eventScheduler: _scheduler)
                .AddTo(_disposables);

            SoundVolume = WindowsObservable.FromEventPattern<MediaPlayer, object>(
                h => _mediaPlayer.VolumeChanged += h,
                h => _mediaPlayer.VolumeChanged -= h
                )
                .Select(x => x.Sender.Volume)
                .ToReadOnlyReactiveProperty(eventScheduler: _scheduler)
                .AddTo(_disposables);
        }

        public IReadOnlyReactiveProperty<MediaPlaybackState> CurrentState { get; private set; }
        public IReadOnlyReactiveProperty<bool> NowBuffering { get; private set; }
        public IReadOnlyReactiveProperty<bool> NowPlaying { get; private set; }

        public IReadOnlyReactiveProperty<bool> IsMuted { get; private set; }
        public IReadOnlyReactiveProperty<double> SoundVolume { get; private set; }

        public IReadOnlyReactiveProperty<double> PlaybackRate { get; private set; }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }

}
