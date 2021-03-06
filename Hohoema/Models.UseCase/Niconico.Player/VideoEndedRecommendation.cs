﻿using Hohoema.Models.Domain;
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
using Uno.Threading;

namespace Hohoema.Models.UseCase.Niconico.Player
{
    public sealed class VideoEndedRecommendation : BindableBase, IDisposable,
        IRecipient<PlaybackStopedMessage>,
        IRecipient<PlaybackStartedMessage>
    {
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private readonly FastAsyncLock _lock = new FastAsyncLock();

        public VideoEndedRecommendation(
            MediaPlayer mediaPlayer,
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
            
            _messenger.RegisterAll(this);
        }

        readonly TimeSpan _endedTime = TimeSpan.FromSeconds(-1);

        bool _endedProcessed;
        private async void PlaybackSession_PositionChanged(MediaPlaybackSession sender, object args)
        {
            using var _ = await _lock.LockAsync(default);

            if (sender.PlaybackState == MediaPlaybackState.None) { return; }
            if (_playNext) { return; }
            if (_hohoemaPlaylistPlayer.CurrentPlaylistItem == null) { return; }
            if (sender.NaturalDuration == TimeSpan.Zero) { return; }

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

                var currentVideoId = _hohoemaPlaylistPlayer.CurrentPlaylistItem.VideoId;
                _queuePlaylist.Remove(currentVideoId);
                _videoPlayedHistoryRepository.VideoPlayed(currentVideoId, sender.Position);

                if (await _hohoemaPlaylistPlayer.GoNextAsync())
                {
                    return;
                }


                if (_playerSettings.IsPlaylistLoopingEnabled)
                {
                    if (await _hohoemaPlaylistPlayer.PlayAsync(_hohoemaPlaylistPlayer.CurrentPlaylist, _hohoemaPlaylistPlayer.CurrentPlaylistSortOption))
                    {
                        return;
                    }
                }

                if (_playerSettings.AutoMoveNextVideoOnPlaylistEmpty)
                {
                    /*
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
                    */

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


        public async void Receive(PlaybackStartedMessage message)
        {
            using var _ = await _lock.LockAsync(default);

            _mediaPlayer.PlaybackSession.PositionChanged -= PlaybackSession_PositionChanged;
            _mediaPlayer.PlaybackSession.PositionChanged += PlaybackSession_PositionChanged;
        }

        public async void Receive(PlaybackStopedMessage message)
        {
            using var _ = await _lock.LockAsync(default);

            _mediaPlayer.PlaybackSession.PositionChanged -= PlaybackSession_PositionChanged;

            var data = message.Value;
            _queuePlaylist.Remove(data.VideoId);
            _videoPlayedHistoryRepository.VideoPlayed(data.VideoId, data.EndPosition);

            _series = null;
            _videoRelatedContents = null;
            HasNextVideo = false;
            NextVideoTitle = null;
            _playNext = false;
            _endedProcessed = false;
            HasRecomend.Value = false;
            _currentVideoDetail = null;
        }


        public async void Dispose()
        {
            using var _ = await _lock.LockAsync(default);

            _messenger.UnregisterAll(this);
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
                        _primaryViewPlayerManager.CloseAsync();
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
