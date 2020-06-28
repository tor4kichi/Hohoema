using Mntone.Nico2.Videos.Dmc;
using Hohoema.Interfaces;
using Hohoema.Models;
using Hohoema.Models.Helpers;
using Hohoema.Models.Niconico;
using Hohoema.Models.Niconico.Video;
using Hohoema.Services;
using Hohoema.UseCase.NicoVideoPlayer.Commands;
using Hohoema.UseCase.Playlist;
using Prism.Commands;
using Prism.Mvvm;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media;
using Windows.Media.Playback;

namespace Hohoema.UseCase
{
    // VideoPlayerの役割
    // 現在再生中のコンテンツの再生を管理
    // 再生中プレイリストの管理
    // 再生するウィンドウの選択を管理

    public class VideoPlayer : BindableBase, IDisposable
    {
        public VideoPlayer(
            MediaPlayer mediaPlayer,
            IScheduler scheduler,
            HohoemaPlaylist hohoemaPlaylist,
            PlayerSettings playerSettings
            )
        {
            _mediaPlayer = mediaPlayer;
            _scheduler = scheduler;
            _hohoemaPlaylist = hohoemaPlaylist;
            _playerSettings = playerSettings;

            // Playing Video
            IsPlayWithCache = new ReactiveProperty<bool>(_scheduler, false)
                .AddTo(_disposables);


            // Playlist
            PlayNextCommand = _hohoemaPlaylist.ObserveProperty(x => x.CanGoNext)
                .ToReactiveCommand(_scheduler)
                .AddTo(_disposables);
            PlayNextCommand.CanExecuteChangedAsObservable()
                .Subscribe(x => Debug.WriteLine("CanPlayNext changed: " + PlayNextCommand.CanExecute()))
                .AddTo(_disposables);
            PlayNextCommand.Subscribe(PlayNext)
                .AddTo(_disposables);
            PlayPreviousCommand = _hohoemaPlaylist.ObserveProperty(x => x.CanGoBack)
                .ToReactiveCommand(_scheduler)
                .AddTo(_disposables);
            PlayPreviousCommand.Subscribe(PlayPrevious)
                .AddTo(_disposables);

            IsShuffleEnabled = _hohoemaPlaylist.ToReactivePropertyAsSynchronized(x => x.IsShuffleEnabled, _scheduler)
                .AddTo(_disposables);
            IsReverseEnabled = _hohoemaPlaylist.ToReactivePropertyAsSynchronized(x => x.IsReverseEnable, _scheduler)
                .AddTo(_disposables);

            IsCurrentVideoLoopingEnabled = _playerSettings.ToReactivePropertyAsSynchronized(x => x.IsCurrentVideoLoopingEnabled, _scheduler)
                .AddTo(_disposables);

            IsCurrentVideoLoopingEnabled.Subscribe(x =>
            {
                _mediaPlayer.IsLoopingEnabled = x;
            })
                .AddTo(_disposables);

            _mediaPlayer.PlaybackSession.PositionChanged += PlaybackSession_PositionChanged;
        }

        TimeSpan? _prevPosition;
        private void PlaybackSession_PositionChanged(MediaPlaybackSession sender, object args)
        {
            _prevPosition = sender.Position;
        }

        CompositeDisposable _disposables = new CompositeDisposable();
        INiconicoVideoSessionProvider _niconicoVideoSessionProvider;
        IStreamingSession _currentSession;
        private readonly MediaPlayer _mediaPlayer;
        private readonly IScheduler _scheduler;

        Models.Helpers.AsyncLock _playerLock = new Models.Helpers.AsyncLock();

        public void Dispose()
        {
            ClearCurrentSessionAsync();

            _mediaPlayer.PlaybackSession.PositionChanged -= PlaybackSession_PositionChanged;
            _disposables.Dispose();
        }

        #region Playing Video


        public IReadOnlyCollection<NicoVideoQualityEntity> AvailableQualities => _niconicoVideoSessionProvider?.AvailableQualities;

        private NicoVideoQualityEntity _currentQuality;
        public NicoVideoQualityEntity CurrentQuality
        {
            get { return _currentQuality; }
            set { SetProperty(ref _currentQuality, value); }
        }

        public ReactiveProperty<bool> IsPlayWithCache { get; }


        private string _playingVideoId;
        public string PlayingVideoId
        {
            get { return _playingVideoId; }
            set { SetProperty(ref _playingVideoId, value); }
        }


        private bool _NowPlayingWithDmcVideo;
        public bool NowPlayingWithDmcVideo
        {
            get { return _NowPlayingWithDmcVideo; }
            private set { SetProperty(ref _NowPlayingWithDmcVideo, value); }
        }


        public async Task UpdatePlayingVideoAsync(INiconicoVideoSessionProvider videoSessionProvider)
        {
            await ClearCurrentSessionAsync();

            using var _ = await _playerLock.LockAsync();

            _niconicoVideoSessionProvider = videoSessionProvider;
            PlayingVideoId = videoSessionProvider.ContentId;
            RaisePropertyChanged(nameof(AvailableQualities));
        }

        public async Task PlayAsync(NicoVideoQuality quality = NicoVideoQuality.Unknown, TimeSpan startPosition = default)
        {
            using var _ = await _playerLock.LockAsync();

            if (_niconicoVideoSessionProvider == null) { throw new ArgumentException("please call VideoPlayer.UpdatePlayingVideo() before VideoPlayer.PlayAsync()."); }

            if ((_currentSession as IVideoStreamingSession)?.Quality == quality)
            {
                return;
            }

            _currentSession?.Dispose();

            _currentSession = await _niconicoVideoSessionProvider.CreateVideoSessionAsync(quality);
            if (_currentSession is IVideoStreamingSession videoStreamingSession)
            {
                CurrentQuality = AvailableQualities.First(x => x.Quality == videoStreamingSession.Quality);
            }
            if (_currentSession is LocalVideoStreamingSession)
            {
                IsPlayWithCache.Value = true;
            }

            NowPlayingWithDmcVideo = _currentSession is Models.DmcVideoStreamingSession;
            
            await _currentSession.StartPlayback(_mediaPlayer, startPosition);
        }

        public async Task ClearCurrentSessionAsync()
        {
            using var _ = await _playerLock.LockAsync();

            _currentSession?.Dispose();
            _currentSession = null;
            _niconicoVideoSessionProvider = null;

            _currentQuality = null;
            RaisePropertyChanged(nameof(AvailableQualities));

            PlayingVideoId = null;
            NowPlayingWithDmcVideo = false;
            IsPlayWithCache.Value = false;
        }


        private DelegateCommand _TogglePlayPauseCommand;
        public DelegateCommand TogglePlayPauseCommand
        {
            get
            {
                return _TogglePlayPauseCommand
                    ?? (_TogglePlayPauseCommand = new DelegateCommand(async () =>
                    {
                        var session = _mediaPlayer.PlaybackSession;
                        if (_mediaPlayer.Source == null 
                        || session.PlaybackState == MediaPlaybackState.None)
                        {
                            await PlayAsync(startPosition: _prevPosition ?? TimeSpan.Zero);
                        }
                        else if (session.PlaybackState == MediaPlaybackState.Playing)
                        {
                            _mediaPlayer.Pause();
                        }
                        else if (session.PlaybackState == MediaPlaybackState.Paused)
                        {
                            _mediaPlayer.Play();
                        }
                    }));
            }
        }

        private DelegateCommand<object> _ChangePlayQualityCommand;
        public DelegateCommand<object> ChangePlayQualityCommand
        {
            get
            {
                return _ChangePlayQualityCommand
                    ?? (_ChangePlayQualityCommand = new DelegateCommand<object>(async (parameter) =>
                    {
                        if (parameter is NicoVideoQualityEntity content)
                        {
                            await PlayAsync(content.Quality, _mediaPlayer.PlaybackSession.Position);
                        }
                    }
                    ));
            }
        }



        #endregion


        #region Playlist

        /* 
         * Note: 
         * 
         * HohoemaPlaylistはメインウィンドウで動作しているが
         * 動画プレイヤーは各ウィンドウで動作している
         * 
	     * そのためコマンドの実行はメインウィンドウで行う必要があるが、
	     * 一方でコマンドの実行可能かをレイズしていいのは各ウィンドウ上に限られる
         * 
         */


        private readonly HohoemaPlaylist _hohoemaPlaylist;
        private readonly PlayerSettings _playerSettings;

        public ReactiveCommand PlayNextCommand { get; }
        public ReactiveCommand PlayPreviousCommand { get; }



        void PlayNext()
        {
            _hohoemaPlaylist.GoNext();
        }

        void PlayPrevious()
        {
            _hohoemaPlaylist.GoBack();
        }



        public ReactiveProperty<bool> IsShuffleEnabled { get; private set; }
        public ReactiveProperty<bool> IsReverseEnabled { get; private set; }
        public ReactiveProperty<bool> IsCurrentVideoLoopingEnabled { get; private set; }
        public ReadOnlyReactiveCollection<IPlaylistItem> PlaylistItems { get; private set; }


        private DelegateCommand _ToggleShuffleCommand;
        public DelegateCommand ToggleShuffleCommand
        {
            get
            {
                return _ToggleShuffleCommand
                    ?? (_ToggleShuffleCommand = new DelegateCommand(() =>
                    {
                        IsShuffleEnabled.Value = !IsShuffleEnabled.Value;
                    }
                    ));
            }
        }



        #endregion

    }
}
