using Hohoema.Models.Domain;
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Domain.Player;
using Hohoema.Models.Domain.Player.Video;
using Hohoema.Models.Domain.Playlist;
using Hohoema.Models.UseCase.Playlist;
using NiconicoToolkit.Video;
using Prism.Commands;
using Prism.Mvvm;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uno.Threading;
using Windows.Media.Playback;

namespace Hohoema.Models.UseCase.Niconico.Player
{
    // VideoPlayerの役割
    // 現在再生中のコンテンツの再生を管理
    // 再生中プレイリストの管理
    // 再生するウィンドウの選択を管理

    public sealed class VideoPlayer : BindableBase, IDisposable
    {
        public VideoPlayer(
            MediaPlayer mediaPlayer,
            IScheduler scheduler,
            PlayerSettings playerSettings,
            HohoemaPlaylistPlayer hohoemaPlaylistPlayer
            )
        {
            _mediaPlayer = mediaPlayer;
            _scheduler = scheduler;
            _playerSettings = playerSettings;
            _hohoemaPlaylistPlayer = hohoemaPlaylistPlayer;

            // Playing Video
            IsPlayWithCache = new ReactiveProperty<bool>(_scheduler, false)
                .AddTo(_disposables);


            // Playlist
            PlayNextCommand = _hohoemaPlaylistPlayer.ObserveProperty(x => x.CurrentPlayingIndex)
                .SelectMany(async x => await _hohoemaPlaylistPlayer.CanGoNextAsync())
                .ToReactiveCommand(_scheduler)
                .AddTo(_disposables);
            PlayNextCommand.Subscribe(async _ => await _hohoemaPlaylistPlayer.GoNextAsync())
                .AddTo(_disposables);
#if DEBUG
            PlayNextCommand.CanExecuteChangedAsObservable()
                .Subscribe(x => Debug.WriteLine("CanPlayNext changed: " + PlayNextCommand.CanExecute()))
                .AddTo(_disposables);
#endif
            PlayPreviousCommand = _hohoemaPlaylistPlayer.ObserveProperty(x => x.CurrentPlayingIndex)
                .SelectMany(async x => await _hohoemaPlaylistPlayer.CanGoPreviewAsync())
                .ToReactiveCommand(_scheduler)
                .AddTo(_disposables);
            PlayPreviousCommand.Subscribe(async _ => await _hohoemaPlaylistPlayer.GoPreviewAsync())
                .AddTo(_disposables);

            IsShuffleEnabled = _hohoemaPlaylistPlayer.ToReactivePropertyAsSynchronized(x => x.IsShuffleModeRequested, _scheduler)
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

        FastAsyncLock _playerLock = new FastAsyncLock();

        public void Dispose()
        {
            _ = ClearCurrentSessionAsync();

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


        private VideoId _playingVideoId;
        public VideoId PlayingVideoId
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


        public async Task UpdatePlayingVideoAsync(INiconicoVideoSessionProvider videoSessionProvider, CancellationToken ct = default)
        {
            await ClearCurrentSessionAsync();

            using var _ = await _playerLock.LockAsync(ct);

            _niconicoVideoSessionProvider = videoSessionProvider;
            PlayingVideoId = videoSessionProvider.ContentId;
            RaisePropertyChanged(nameof(AvailableQualities));
        }

        public async Task PlayAsync(NicoVideoQuality quality = NicoVideoQuality.Unknown, TimeSpan startPosition = default, CancellationToken ct = default)
        {
            using var _ = await _playerLock.LockAsync(ct);

            if (_niconicoVideoSessionProvider == null) { throw new ArgumentException("please call VideoPlayer.UpdatePlayingVideo() before VideoPlayer.PlayAsync()."); }

            if ((_currentSession as IVideoStreamingSession)?.Quality == quality)
            {
                return;
            }

            if (quality == NicoVideoQuality.Unknown)
            {
                quality = _playerSettings.DefaultVideoQuality;
            }

            _currentSession?.Dispose();

            _currentSession = await _niconicoVideoSessionProvider.CreateVideoSessionAsync(quality);
            if (_currentSession is IVideoStreamingSession videoStreamingSession)
            {
                CurrentQuality = AvailableQualities.First(x => x.Quality == videoStreamingSession.Quality);
            }
            if (_currentSession is CachedVideoStreamingSession)
            {
                IsPlayWithCache.Value = true;
            }

            NowPlayingWithDmcVideo = _currentSession is DmcVideoStreamingSession;

            await _currentSession.StartPlayback(_mediaPlayer, startPosition);
        }

        public async Task ClearCurrentSessionAsync(CancellationToken ct = default)
        {
            using var _ = await _playerLock.LockAsync(ct);

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
                        try
                        {
                            
                        }
                        catch (Exception e)
                        {
                            ErrorTrackingManager.TrackError(e);
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
                        try
                        {
                            if (parameter is NicoVideoQualityEntity content)
                            {
                                await PlayAsync(content.Quality, _mediaPlayer.PlaybackSession.Position);
                                _playerSettings.DefaultVideoQuality = content.Quality;
                            }
                        }
                        catch (Exception e)
                        {
                            ErrorTrackingManager.TrackError(e);
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


        private readonly PlayerSettings _playerSettings;
        private readonly HohoemaPlaylistPlayer _hohoemaPlaylistPlayer;

        public ReactiveCommand PlayNextCommand { get; }
        public ReactiveCommand PlayPreviousCommand { get; }



        public ReactiveProperty<bool> IsShuffleEnabled { get; private set; }
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
