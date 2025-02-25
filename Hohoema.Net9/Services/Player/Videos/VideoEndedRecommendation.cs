﻿#nullable enable
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Hohoema.Models.Application;
using Hohoema.Models.Niconico.Video;
using Hohoema.Models.Player;
using Hohoema.Models.Player.Video;
using Hohoema.Models.Playlist;
using NiconicoToolkit.FollowingsActivity;
using NiconicoToolkit.Video;
using NiconicoToolkit.Video.Watch;
using Reactive.Bindings;
using System;
using System.Diagnostics;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Windows.Media.Playback;
using Windows.System;
using static NiconicoToolkit.Video.Watch.NicoVideoWatchApiResponse;

namespace Hohoema.Services.Player;

public sealed class VideoEndedRecommendation : ObservableObject, IDisposable,
    IRecipient<PlaybackStopedMessage>,
    IRecipient<PlaybackStartedMessage>,
    IRecipient<PlaybackFailedMessage>
{
    private readonly CompositeDisposable _disposables = new CompositeDisposable();
    private readonly Helpers.AsyncLock _lock = new Helpers.AsyncLock();

    public VideoEndedRecommendation(
        MediaPlayer mediaPlayer,
        IMessenger messenger,
        IScheduler scheduler,
        QueuePlaylist queuePlaylist,
        RelatedVideoContentsAggregator relatedVideoContentsAggregator,
        PrimaryViewPlayerManager primaryViewPlayerManager,
        PlayerSettings playerSettings,
        AppearanceSettings appearanceSettings,
        HohoemaPlaylistPlayer hohoemaPlaylistPlayer,
        VideoWatchedRepository videoWatchedRepository
        )
    {
        _mediaPlayer = mediaPlayer;
        _messenger = messenger;
        _scheduler = scheduler;
        _queuePlaylist = queuePlaylist;
        _relatedVideoContentsAggregator = relatedVideoContentsAggregator;
        _primaryViewPlayerManager = primaryViewPlayerManager;
        _playerSettings = playerSettings;
        _appearanceSettings = appearanceSettings;
        _hohoemaPlaylistPlayer = hohoemaPlaylistPlayer;
        _videoWatchedRepository = videoWatchedRepository;
        IsEnded = new ReactiveProperty<bool>(_scheduler);
        HasRecomend = new ReactiveProperty<bool>(_scheduler);
        
        _messenger.RegisterAll(this);

        _positionUpdateTimer = DispatcherQueue.GetForCurrentThread().CreateTimer();
        _positionUpdateTimer.Interval = TimeSpan.FromMilliseconds(250);
        _positionUpdateTimer.IsRepeating = true;
        _positionUpdateTimer.Tick += async (t, s) =>
        {
            using var _ = await _lock.LockAsync(default);

            var sender = _mediaPlayer.PlaybackSession;
            if (sender.PlaybackState == MediaPlaybackState.None) { return; }
            if (_playNext) { return; }
            if (_hohoemaPlaylistPlayer.CurrentPlaylistItem == null) { return; }
            if (sender.NaturalDuration == TimeSpan.Zero) { return; }

            bool isInsideEndedRange = sender.Position - sender.NaturalDuration > _endedTime;
            bool isStopped = sender.PlaybackState == MediaPlaybackState.Paused;
            IsEnded.Value = isInsideEndedRange && isStopped;
            
            if (HasRecomend.Value) { return; }

            if (!IsEnded.Value || _endedProcessed)
            {
                HasRecomend.Value = HasNextVideo && IsEnded.Value;
                return;
            }

            var currentVideoId = _hohoemaPlaylistPlayer.CurrentPlaylistItem.VideoId;
            // この時点でキューから削除してしまうと、プレイヤー上のプレイリストの現在アイテムがnullになってしまう
            // 他のアイテムが再生されたり、プレイヤーが閉じられたタイミングで
            //
            //_queuePlaylist.Remove(currentVideoId);
            //_videoWatchedRepository.VideoPlayed(currentVideoId, sender.Position);

            if (_playerSettings.IsPlaylistAutoMoveEnabled && await _hohoemaPlaylistPlayer.CanGoNextAsync())
            {
                if (await _hohoemaPlaylistPlayer.GoNextAsync())
                {
                    return;
                }
            }

            // _queuePlaylistのアイテム削除後の更新がScheduler上で行われるため、アイテム更新操作が同期的にではなく
            // sortablePlaylist.TotalCountが実際とズレる可能性があるため、処理完了を待つ
            await Task.Delay(10);

            if (_playerSettings.IsPlaylistLoopingEnabled
                && _hohoemaPlaylistPlayer.CurrentPlaylist is ISortablePlaylist sortablePlaylist
                && sortablePlaylist.TotalCount > 0
                && _hohoemaPlaylistPlayer.CurrentPlaylist is not QueuePlaylist
                )
            {
                if (await _hohoemaPlaylistPlayer.PlayAsync(_hohoemaPlaylistPlayer.CurrentPlaylist, _hohoemaPlaylistPlayer.CurrentPlaylistSortOption))
                {
                    return;
                }
            }


            if (_hohoemaPlaylistPlayer.CurrentPlaylistItem == null && _videoRelatedContents?.NextVideo != null)
            {
                _endedProcessed = true;
                HasNextVideo = _videoRelatedContents?.NextVideo != null;
                NextVideoTitle = _videoRelatedContents?.NextVideo?.Label;
                HasRecomend.Value = HasNextVideo && IsEnded.Value;
                return;
            }

            if (_series?.Video.Next is not null and var nextVideo)
            {
                NextVideoTitle = nextVideo.Title;
                HasRecomend.Value = true;
                HasNextVideo = true;

                Debug.WriteLine("シリーズ情報から次の動画を提示: " + nextVideo.Title);
                return;
            }

            if (TryPlaylistEndActionPlayerClosed())
            {
                HasRecomend.Value = HasNextVideo && IsEnded.Value;
                _endedProcessed = true;
                return;
            }

            if (_currentVideoDetail != null)
            {
                _videoRelatedContents ??= await _relatedVideoContentsAggregator.GetRelatedContentsAsync(_currentVideoDetail);
                HasNextVideo = _videoRelatedContents.NextVideo != null;
                NextVideoTitle = _videoRelatedContents.NextVideo?.Label;
                HasRecomend.Value = HasNextVideo && IsEnded.Value;

                Debug.WriteLine("動画情報から次の動画を提示: " + NextVideoTitle);
            }
        };
    }

    readonly TimeSpan _endedTime = TimeSpan.FromSeconds(-1);

    bool _endedProcessed;

    public async void Receive(PlaybackStartedMessage message)
    {
        using var _ = await _lock.LockAsync(default);

        if (_failedVideoId != null)
        {
            _queuePlaylist.Remove(_failedVideoId.Value);
            _failedVideoId = null;
        }

        _series = null;
        _videoRelatedContents = null;
        HasNextVideo = false;
        NextVideoTitle = null;
        _playNext = false;
        _endedProcessed = false;
        HasRecomend.Value = false;
        _currentVideoDetail = null;
        _positionUpdateTimer.Start();
    }

    public async void Receive(PlaybackStopedMessage message)
    {
        using var _ = await _lock.LockAsync(default);

        _positionUpdateTimer.Stop();

        var data = message.Value;
        _queuePlaylist.Remove(data.VideoId);
        _videoWatchedRepository.VideoPlayed(data.VideoId, data.EndPosition);
    }


    public async void Receive(PlaybackFailedMessage message)
    {
        using var _ = await _lock.LockAsync(default);

        _positionUpdateTimer.Stop();

        var data = message.Value;
        _failedVideoId = data.VideoId;
    }

    VideoId? _failedVideoId;

    public async void Dispose()
    {
        using var _ = await _lock.LockAsync(default);

        _messenger.UnregisterAll(this);
        _disposables.Dispose();
        _positionUpdateTimer.Stop();
    }

    bool TryPlaylistEndActionPlayerClosed()
    {
        if (_appearanceSettings.PlayerDisplayView == PlayerDisplayView.PrimaryView)
        {
            switch (_playerSettings.PlaylistEndAction)
            {
                case PlaylistEndAction.ChangeIntoSplit:
                    _primaryViewPlayerManager.ShowWithWindowInWindowAsync();
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


    VideoRelatedContents? _videoRelatedContents;

    private readonly MediaPlayer _mediaPlayer;
    private readonly IMessenger _messenger;
    private readonly IScheduler _scheduler;
    private readonly QueuePlaylist _queuePlaylist;
    private readonly RelatedVideoContentsAggregator _relatedVideoContentsAggregator;
    private readonly PrimaryViewPlayerManager _primaryViewPlayerManager;
    private readonly PlayerSettings _playerSettings;
    private readonly AppearanceSettings _appearanceSettings;
    private readonly HohoemaPlaylistPlayer _hohoemaPlaylistPlayer;
    private readonly VideoWatchedRepository _videoWatchedRepository;
    private readonly DispatcherQueueTimer _positionUpdateTimer;

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

    private RelayCommand _playNextVideoCommand;
    public RelayCommand PlayNextVideoCommand => _playNextVideoCommand
        ?? (_playNextVideoCommand = new RelayCommand(() =>
        {
            if (_videoRelatedContents?.NextVideo != null)
            {
                var nextVideo = _videoRelatedContents.NextVideo;
                _messenger.Send(VideoPlayRequestMessage.PlayVideo(nextVideo.VideoId));
            }
            else if (_series.Video.Next is not null and var nextVideo)
            {
                _messenger.Send(VideoPlayRequestMessage.PlayVideo(nextVideo.Id));
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


    private RelayCommand _CanceledNextPartMoveCommand;
    public RelayCommand CanceledNextPartMoveCommand =>
        _CanceledNextPartMoveCommand ?? (_CanceledNextPartMoveCommand = new RelayCommand(ExecuteCanceledNextPartMoveCommand));

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
