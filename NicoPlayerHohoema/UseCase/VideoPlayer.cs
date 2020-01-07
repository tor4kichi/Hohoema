using Mntone.Nico2.Videos.Dmc;
using NicoPlayerHohoema.Interfaces;
using NicoPlayerHohoema.Models;
using NicoPlayerHohoema.Models.Niconico;
using NicoPlayerHohoema.Models.Niconico.Video;
using NicoPlayerHohoema.Services;
using NicoPlayerHohoema.UseCase.NicoVideoPlayer.Commands;
using NicoPlayerHohoema.UseCase.Playlist;
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

namespace NicoPlayerHohoema.UseCase
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
            PlayerViewManager playerViewManager
            )
        {
            _mediaPlayer = mediaPlayer;
            _scheduler = scheduler;
            _hohoemaPlaylist = hohoemaPlaylist;
            _playerViewManager = playerViewManager;

            // Playing Video
            IsPlayWithCache = new ReactiveProperty<bool>(_scheduler, false)
                .AddTo(_disposables);
            SeekCommand = new NicoVideoPlayer.Commands.MediaPlayerSeekCommand(_mediaPlayer);
            SetPlaybackRateCommand = new MediaPlayerSetPlaybackRateCommand(_mediaPlayer);
            ToggleMuteCommand = new MediaPlayerToggleMuteCommand(_mediaPlayer);
            VolumeUpCommand = new MediaPlayerVolumeUpCommand(_mediaPlayer);
            VolumeDownCommand = new MediaPlayerVolumeDownCommand(_mediaPlayer);



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

            RepeatMode = _hohoemaPlaylist.ToReactivePropertyAsSynchronized(x => x.RepeatMode, _scheduler)
                .AddTo(_disposables);
            RepeatMode.Select(x => x == MediaPlaybackAutoRepeatMode.Track)
                .Subscribe(x => _mediaPlayer.IsLoopingEnabled = x)
                .AddTo(_disposables);

            
        }



        CompositeDisposable _disposables = new CompositeDisposable();
        INiconicoVideoSessionProvider _niconicoVideoSessionProvider;
        IStreamingSession _currentSession;
        private readonly MediaPlayer _mediaPlayer;
        private readonly IScheduler _scheduler;


        public void Dispose()
        {
            ClearCurrentSession();

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


        public void UpdatePlayingVideo(INiconicoVideoSessionProvider videoSessionProvider)
        {
            ClearCurrentSession();

            _niconicoVideoSessionProvider = videoSessionProvider;
            PlayingVideoId = videoSessionProvider.ContentId;
            RaisePropertyChanged(nameof(AvailableQualities));
        }

        public async Task PlayAsync(NicoVideoQuality quality = NicoVideoQuality.Unknown, TimeSpan startPosition = default)
        {
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

        public void ClearCurrentSession()
        {
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
                        if (session.PlaybackState == MediaPlaybackState.None)
                        {
                            await PlayAsync(startPosition: session.Position);
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



        public MediaPlayerSeekCommand SeekCommand { get; }
        public MediaPlayerSetPlaybackRateCommand SetPlaybackRateCommand { get; }
        public MediaPlayerToggleMuteCommand ToggleMuteCommand { get; }
        public MediaPlayerVolumeUpCommand VolumeUpCommand { get; }
        public MediaPlayerVolumeDownCommand VolumeDownCommand { get; }
        
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
        private readonly PlayerViewManager _playerViewManager;

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
        public ReactiveProperty<MediaPlaybackAutoRepeatMode> RepeatMode { get; private set; }
        public ReadOnlyReactiveCollection<IPlaylistItem> PlaylistItems { get; private set; }


        // TODO: HohoemaPlaylistへの書き込みに差し替え（PlayerSettingsへの書き戻しはObserverを立てる）

        private DelegateCommand _ToggleRepeatModeCommand;
        public DelegateCommand ToggleRepeatModeCommand
        {
            get
            {
                return _ToggleRepeatModeCommand
                    ?? (_ToggleRepeatModeCommand = new DelegateCommand(() =>
                    {
                        switch (RepeatMode.Value)
                        {
                            case MediaPlaybackAutoRepeatMode.None:
                                RepeatMode.Value = MediaPlaybackAutoRepeatMode.Track;
                                break;
                            case MediaPlaybackAutoRepeatMode.Track:
                                RepeatMode.Value = MediaPlaybackAutoRepeatMode.List;
                                break;
                            case MediaPlaybackAutoRepeatMode.List:
                                RepeatMode.Value = MediaPlaybackAutoRepeatMode.None;
                                break;
                            default:
                                break;
                        }
                    }
                    ));
            }
        }

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
