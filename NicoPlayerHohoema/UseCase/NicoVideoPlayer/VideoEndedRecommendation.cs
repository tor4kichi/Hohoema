using NicoPlayerHohoema.Models;
using NicoPlayerHohoema.Services;
using NicoPlayerHohoema.Services.Player;
using NicoPlayerHohoema.UseCase.Playlist;
using Prism.Commands;
using Prism.Mvvm;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Media.Playback;
using Windows.UI.Xaml;

namespace NicoPlayerHohoema.UseCase.NicoVideoPlayer
{
    public sealed class VideoEndedRecommendation : BindableBase, IDisposable
    {
        CompositeDisposable _disposables = new CompositeDisposable();

        public VideoEndedRecommendation(
            MediaPlayer mediaPlayer,
            VideoPlayer videoPlayer,
            IScheduler scheduler,
            RelatedVideoContentsAggregator relatedVideoContentsAggregator,
            HohoemaPlaylist hohoemaPlaylist,
            PrimaryViewPlayerManager primaryViewPlayerManager,
            PlayerSettings playerSettings,
            VideoPlayRequestBridgeToPlayer videoPlayRequestBridgeToPlayer
            )
        {
            _mediaPlayer = mediaPlayer;
            _videoPlayer = videoPlayer;
            _scheduler = scheduler;
            _relatedVideoContentsAggregator = relatedVideoContentsAggregator;
            _hohoemaPlaylist = hohoemaPlaylist;
            _primaryViewPlayerManager = primaryViewPlayerManager;
            _playerSettings = playerSettings;
            _videoPlayRequestBridgeToPlayer = videoPlayRequestBridgeToPlayer;
            IsEnded = new ReactiveProperty<bool>(_scheduler);
            HasRecomend = new ReactiveProperty<bool>(_scheduler);
            
            _mediaPlayer.PlaybackSession.PositionChanged += PlaybackSession_PositionChanged;

            _videoPlayer.ObserveProperty(x => x.PlayingVideoId)
                .Subscribe(x =>
                {
                    _videoRelatedContents = null;
                    HasNextVideo = false;
                    NextVideoTitle = null;
                    _playNext = false;
                    _endedProcessed = false;
                    HasRecomend.Value = false;
                })
                .AddTo(_disposables);
        }

        readonly TimeSpan _endedTime = TimeSpan.FromSeconds(-1);

        bool _endedProcessed;
        private void PlaybackSession_PositionChanged(MediaPlaybackSession sender, object args)
        {
            if (sender.PlaybackState == MediaPlaybackState.None) { return; }
            if (_playNext) { return; }

            if (sender.Position - sender.NaturalDuration > _endedTime)
            {
                if (_videoRelatedContents == null 
                    && IsEnded.Value == false
                    && !_endedProcessed
                    )
                {
                    _endedProcessed = true;
                    if (!TryPlaylistEndActionPlayerClosed())
                    {
                        if (!_hohoemaPlaylist.PlayDoneAndTryMoveNext())
                        {
                            if (_videoPlayer.PlayingVideoId == null) 
                            {
                                HasNextVideo = _videoRelatedContents.NextVideo != null;
                                NextVideoTitle = _videoRelatedContents.NextVideo?.Label;
                                return; 
                            }
                            _relatedVideoContentsAggregator.GetRelatedContentsAsync(_videoPlayer.PlayingVideoId)
                                .ContinueWith(async task =>
                                {
                                    var relatedVideos = await task;

                                    _scheduler.Schedule(() =>
                                    {
                                        _videoRelatedContents = relatedVideos;
                                        HasNextVideo = _videoRelatedContents.NextVideo != null;
                                        NextVideoTitle = _videoRelatedContents.NextVideo?.Label;
                                        HasRecomend.Value = HasNextVideo && IsEnded.Value;
                                    });
                                });
                        }
                    }
                }

                IsEnded.Value = sender.PlaybackState == MediaPlaybackState.None 
                    || sender.PlaybackState == MediaPlaybackState.Paused;
            }
            else
            {
                IsEnded.Value = false;
            }

            HasRecomend.Value = HasNextVideo && IsEnded.Value;
        }

        public void Dispose()
        {
            _mediaPlayer.PlaybackSession.PositionChanged -= PlaybackSession_PositionChanged;

            _disposables.Dispose();
        }

        bool TryPlaylistEndActionPlayerClosed()
        {
            if (_videoPlayRequestBridgeToPlayer.DisplayMode == PlayerDisplayView.PrimaryView)
            {
                switch (_playerSettings.PlaylistEndAction)
                {
                    case PlaylistEndAction.ChangeIntoSplit:
                        _primaryViewPlayerManager.ShowWithWindowInWindow();
                        return true;
                    case PlaylistEndAction.CloseIfPlayWithCurrentWindow:
                        _primaryViewPlayerManager.Close();
                        return true;
                }

                return false;
            }
            else
            {
                return false;
            }
        }


        VideoRelatedContents _videoRelatedContents;

        private readonly MediaPlayer _mediaPlayer;
        private readonly VideoPlayer _videoPlayer;
        private readonly IScheduler _scheduler;
        private readonly RelatedVideoContentsAggregator _relatedVideoContentsAggregator;
        private readonly HohoemaPlaylist _hohoemaPlaylist;
        private readonly PrimaryViewPlayerManager _primaryViewPlayerManager;
        private readonly PlayerSettings _playerSettings;
        private readonly VideoPlayRequestBridgeToPlayer _videoPlayRequestBridgeToPlayer;

        public ReactiveProperty<bool> IsEnded { get; }

        public ReactiveProperty<bool> HasRecomend { get; }

        private string _nextVideoTitle;
        public string NextVideoTitle
        {
            get { return _nextVideoTitle; }
            set { SetProperty(ref _nextVideoTitle, value); }
        }


        private bool _hasNextVideo;
        public bool HasNextVideo
        {
            get { return _hasNextVideo; }
            set { SetProperty(ref _hasNextVideo, value); }
        }

        bool _playNext;

        private DelegateCommand _playNextVideoCommand;
        public DelegateCommand PlayNextVideoCommand => _playNextVideoCommand
            ?? (_playNextVideoCommand = new DelegateCommand(() =>
            {
                if (_videoRelatedContents?.NextVideo == null) { return; }

                var nextVideo = _videoRelatedContents.NextVideo;
                _hohoemaPlaylist.Play(nextVideo);
                IsEnded.Value = false;
                _playNext = true;
                HasRecomend.Value = false;
            }));
    }
}
