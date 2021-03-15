using Mntone.Nico2.Videos.Dmc;
using Hohoema.Models.Domain;
using Hohoema.Presentation.Services;
using Hohoema.Presentation.Services.Player;
using Hohoema.Models.UseCase.NicoVideos;
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

namespace Hohoema.Models.UseCase.NicoVideos.Player
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
                    _series = null;
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

                if (_hohoemaPlaylist.PlayDoneAndTryMoveNext(_mediaPlayer.PlaybackSession.Position))
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

                            Debug.WriteLine("動画情報から次の動画を提示: " + NextVideoTitle);
                        });
                    });
            }
            finally
            {
                
            }
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
                if (_videoRelatedContents?.NextVideo != null)
                {
                    var nextVideo = _videoRelatedContents.NextVideo;
                    _hohoemaPlaylist.Play(nextVideo);
                }
                else if (_series.Video.Next is not null and var nextVideo)
                {
                    _hohoemaPlaylist.Play(nextVideo.Id);
                }

                IsEnded.Value = false;
                _playNext = true;
                HasRecomend.Value = false;
            }));

        Series _series;
        public void SetCurrentVideoSeries(Series series)
        {
            _series = series;
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
