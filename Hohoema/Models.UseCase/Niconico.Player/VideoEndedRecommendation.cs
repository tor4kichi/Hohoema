using Hohoema.Models.Domain;
using Hohoema.Presentation.Services;
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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Media.Playback;
using Windows.UI.Xaml;
using Hohoema.Models.Domain.Player;
using NiconicoToolkit.Video.Watch;
using Hohoema.Models.Domain.Player.Video;
using Hohoema.Models.UseCase.Playlist;
using Hohoema.Models.Domain.Playlist;
using Microsoft.Toolkit.Mvvm.Messaging;
using Hohoema.Models.Domain.Niconico.Video.WatchHistory.LoginUser;
using NiconicoToolkit.Video;

namespace Hohoema.Models.UseCase.Niconico.Player
{
    public sealed class VideoEndedRecommendation : BindableBase, IDisposable,
        IRecipient<PlaybackStopedMessage>
    {
        CompositeDisposable _disposables = new CompositeDisposable();

        public VideoEndedRecommendation(
            MediaPlayer mediaPlayer,
            VideoPlayer videoPlayer,
            IMessenger messenger,
            IScheduler scheduler,
            QueuePlaylist queuePlaylist,
            RelatedVideoContentsAggregator relatedVideoContentsAggregator,
            PrimaryViewPlayerManager primaryViewPlayerManager,
            PlayerSettings playerSettings,
            VideoPlayRequestBridgeToPlayer videoPlayRequestBridgeToPlayer,
            HohoemaPlaylistPlayer hohoemaPlaylistPlayer,
            VideoPlayedHistoryRepository videoPlayedHistoryRepository
            )
        {
            _mediaPlayer = mediaPlayer;
            _videoPlayer = videoPlayer;
            _messenger = messenger;
            _scheduler = scheduler;
            _queuePlaylist = queuePlaylist;
            _relatedVideoContentsAggregator = relatedVideoContentsAggregator;
            _primaryViewPlayerManager = primaryViewPlayerManager;
            _playerSettings = playerSettings;
            _videoPlayRequestBridgeToPlayer = videoPlayRequestBridgeToPlayer;
            _hohoemaPlaylistPlayer = hohoemaPlaylistPlayer;
            _videoPlayedHistoryRepository = videoPlayedHistoryRepository;
            IsEnded = new ReactiveProperty<bool>(_scheduler);
            HasRecomend = new ReactiveProperty<bool>(_scheduler);
            
            _mediaPlayer.PlaybackSession.PositionChanged += PlaybackSession_PositionChanged;

            _videoPlayer.ObserveProperty(x => x.PlayingVideoId)
                .Subscribe(x =>
                {
                    _series = null;
                    _videoRelatedContents = null;
                    HasNextVideo = false;
                    NextVideoTitle = null;
                    _playNext = false;
                    _endedProcessed = false;
                    HasRecomend.Value = false;
                    _currentVideoDetail = null;
                })
                .AddTo(_disposables);

            _messenger.Register<PlaybackStopedMessage>(this);
        }

        readonly TimeSpan _endedTime = TimeSpan.FromSeconds(-1);

        bool _endedProcessed;
        private async void PlaybackSession_PositionChanged(MediaPlaybackSession sender, object args)
        {
            if (sender.PlaybackState == MediaPlaybackState.None) { return; }
            if (_playNext) { return; }
            if (_videoPlayer.PlayingVideoId == default(VideoId)) { return; }

            bool isInsideEndedRange = sender.Position - sender.NaturalDuration > _endedTime;
            bool isStopped = sender.PlaybackState == MediaPlaybackState.Paused;
            IsEnded.Value = isInsideEndedRange && isStopped;

            try
            {
                if (!IsEnded.Value || _endedProcessed)
                {
                    HasRecomend.Value = HasNextVideo && IsEnded.Value;
                    return;
                }
                
                _queuePlaylist.Remove(_videoPlayer.PlayingVideoId);
                _videoPlayedHistoryRepository.VideoPlayed(_videoPlayer.PlayingVideoId, sender.Position);

                if (await _hohoemaPlaylistPlayer.GoNextAsync())
                {
                    HasRecomend.Value = HasNextVideo && IsEnded.Value;
                    _endedProcessed = true;
                    return;
                }

                if (_playerSettings.AutoMoveNextVideoOnPlaylistEmpty)
                {
                    if (_videoPlayer.PlayingVideoId == null)
                    {
                        _endedProcessed = true;
                        _scheduler.Schedule(() =>
                        {
                            HasNextVideo = _videoRelatedContents?.NextVideo != null;
                            NextVideoTitle = _videoRelatedContents?.NextVideo?.Label;
                            HasRecomend.Value = HasNextVideo && IsEnded.Value;
                        });
                        return;
                    }

                    if (_series?.Video.Next is not null and var nextVideo)
                    {
                        _scheduler.Schedule(() =>
                        {
                            HasNextVideo = true;
                            NextVideoTitle = nextVideo.Title;
                            HasRecomend.Value = true;

                            Debug.WriteLine("シリーズ情報から次の動画を提示: " + nextVideo.Title);
                        });

                        return;
                    }
                }

                if (TryPlaylistEndActionPlayerClosed())
                {
                    HasRecomend.Value = HasNextVideo && IsEnded.Value;
                    _endedProcessed = true;
                    return;
                }

                if (_currentVideoDetail != null)
                {
                    _relatedVideoContentsAggregator.GetRelatedContentsAsync(_currentVideoDetail)
                        .ContinueWith(async task =>
                        {
                            var relatedVideos = await task;

                            _scheduler.Schedule(() =>
                            {
                                _videoRelatedContents = relatedVideos;
                                HasNextVideo = _videoRelatedContents.NextVideo != null;
                                NextVideoTitle = _videoRelatedContents.NextVideo?.Label;
                                HasRecomend.Value = HasNextVideo && IsEnded.Value;

                                Debug.WriteLine("動画情報から次の動画を提示: " + NextVideoTitle);
                            });
                        });
                }
            }
            finally
            {
                
            }
        }

        public void Receive(PlaybackStopedMessage message)
        {
            var data = message.Value;
            _queuePlaylist.Remove(data.VideoId);
            _videoPlayedHistoryRepository.VideoPlayed(data.VideoId, data.EndPosition);
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
        private readonly IMessenger _messenger;
        private readonly IScheduler _scheduler;
        private readonly QueuePlaylist _queuePlaylist;
        private readonly RelatedVideoContentsAggregator _relatedVideoContentsAggregator;
        private readonly PrimaryViewPlayerManager _primaryViewPlayerManager;
        private readonly PlayerSettings _playerSettings;
        private readonly VideoPlayRequestBridgeToPlayer _videoPlayRequestBridgeToPlayer;
        private readonly HohoemaPlaylistPlayer _hohoemaPlaylistPlayer;
        private readonly VideoPlayedHistoryRepository _videoPlayedHistoryRepository;

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
                if (_videoRelatedContents?.NextVideo != null)
                {
                    var nextVideo = _videoRelatedContents.NextVideo;
                    _messenger.Send(new VideoPlayRequestMessage() { VideoId = nextVideo.VideoId });
                }
                else if (_series.Video.Next is not null and var nextVideo)
                {
                    _messenger.Send(new VideoPlayRequestMessage() { VideoId = nextVideo.Id });
                }

                IsEnded.Value = false;
                _playNext = true;
                HasRecomend.Value = false;
            }));

        INicoVideoDetails _currentVideoDetail;
        WatchApiSeries _series;
        public void SetCurrentVideoSeries(INicoVideoDetails videoDetail)
        {
            _currentVideoDetail = videoDetail;
            _series = _currentVideoDetail.Series;
        }


        private DelegateCommand _CanceledNextPartMoveCommand;
        public DelegateCommand CanceledNextPartMoveCommand =>
            _CanceledNextPartMoveCommand ?? (_CanceledNextPartMoveCommand = new DelegateCommand(ExecuteCanceledNextPartMoveCommand));

        void ExecuteCanceledNextPartMoveCommand()
        {
            if (TryPlaylistEndActionPlayerClosed())
            {
                HasRecomend.Value = HasNextVideo && IsEnded.Value;
                _endedProcessed = true;
                return;
            }
        }

    }
}
