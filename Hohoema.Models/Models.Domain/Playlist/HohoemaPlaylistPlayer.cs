using Hohoema.Models.Domain.Niconico.Player;
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Domain.PageNavigation;
using Hohoema.Models.Domain.Player;
using Hohoema.Models.Domain.Player.Video;
using Hohoema.Models.Domain.VideoCache;
using Hohoema.Models.Helpers;
using Hohoema.Models.Infrastructure;
using Microsoft.Toolkit.Collections;
using Microsoft.Toolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.Toolkit.Uwp;
using NiconicoToolkit.Video;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Media;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage.Streams;
using Windows.System;
using static Hohoema.Models.Domain.Niconico.Player.VideoStreamingOriginOrchestrator;

namespace Hohoema.Models.Domain.Playlist
{
    public record PlaybackStartedMessageData(HohoemaPlaylistPlayer Player, PlaylistId PlaylistId, VideoId VideoId, NicoVideoQuality VideoQuality, MediaPlaybackSession Session);

    public sealed class PlaybackStartedMessage : ValueChangedMessage<PlaybackStartedMessageData>
    {
        public PlaybackStartedMessage(PlaybackStartedMessageData value) : base(value)
        {
        }
    }


    public record PlaybackFailedMessageData(HohoemaPlaylistPlayer Player, PlaylistId PlaylistId, VideoId VideoId, PlayingOrchestrateFailedReason FailedReason);

    public sealed class PlaybackFailedMessage : ValueChangedMessage<PlaybackFailedMessageData>
    {
        public PlaybackFailedMessage(PlaybackFailedMessageData value) : base(value)
        {
        }
    }
    public enum PlaybackStopReason
    {
        FromUser,
    }

    public record PlaybackStopedMessageData(HohoemaPlaylistPlayer Player, PlaylistId PlaylistId, VideoId VideoId, TimeSpan EndPosition, PlaybackStopReason StopReason);

    public sealed class PlaybackStopedMessage : ValueChangedMessage<PlaybackStopedMessageData>
    {
        public PlaybackStopedMessage(PlaybackStopedMessageData value) : base(value)
        {
        }
    }

    public record PlayingPlaylistChangedMessageData(HohoemaPlaylistPlayer Player, IPlaylist? PlaylistItemsSource);

    public sealed class PlayingPlaylistChangedMessage : ValueChangedMessage<PlayingPlaylistChangedMessageData>
    {
        public PlayingPlaylistChangedMessage(PlayingPlaylistChangedMessageData value) : base(value)
        {
        }
    }

    public record ResolvePlaylistFailedMessageData(HohoemaPlaylistPlayer Player, PlaylistId PlaylistId);

    public sealed class ResolvePlaylistFailedMessage : ValueChangedMessage<ResolvePlaylistFailedMessageData>
    {
        public ResolvePlaylistFailedMessage(ResolvePlaylistFailedMessageData value) : base(value)
        {
        }
    }



    public abstract class PlaylistPlayer : ObservableObject, IDisposable
    {
        private const int InvalidIndex = -1;
        private readonly PlayerSettings _playerSettings;
        private readonly IScheduler _scheduler;


        private IBufferedPlaylistItemsSource? _bufferedPlaylistItemsSource;
        protected IBufferedPlaylistItemsSource BufferedPlaylistItemsSource
        {
            get => _bufferedPlaylistItemsSource;
            private set => SetProperty(ref _bufferedPlaylistItemsSource, value);
        }

        IDisposable _IndexUpdateTimingSubscriber;
        IDisposable _IsShuffleEnableSubscriber;
        public PlaylistPlayer(PlayerSettings playerSettings, IScheduler scheduler)
        {
            _playerSettings = playerSettings;
            _scheduler = scheduler;

            _IsShuffleEnableSubscriber = _playerSettings.ObserveProperty(x => x.IsShuffleEnable)
                .Subscribe(enabled =>
                {
                    _scheduler.Schedule(() => OnPropertyChanged(nameof(IsShuffleModeRequested)));
                });
        }

        private int[] _shuffledIndexies;
        private int? _maxItemsCount;        

        private IPlaylist _currentPlaylist;
        public IPlaylist CurrentPlaylist
        {
            get { return _currentPlaylist; }
            protected set 
            {
                if (SetProperty(ref _currentPlaylist, value))
                {
                    OnPropertyChanged(nameof(CurrentPlaylistSortOptions));
                }
            }
        }

        public IPlaylistSortOption[] CurrentPlaylistSortOptions => (CurrentPlaylist as IUserManagedPlaylist)?.SortOptions;

        public IPlaylistSortOption CurrentPlaylistSortOption { get; private set; }


        private bool _isUnlimitedPlaylistSource;
        public bool IsUnlimitedPlaylistSource
        {
            get { return _isUnlimitedPlaylistSource; }
            set
            {
                if (SetProperty(ref _isUnlimitedPlaylistSource, value))
                {
                    OnPropertyChanged(nameof(IsShuffleAndRepeatAvailable));
                    OnPropertyChanged(nameof(IsShuffleModeEnabled));
                }
            }
        }

        private int _currentPlayingIndex;
        public int CurrentPlayingIndex
        {
            get { return _currentPlayingIndex; }
            protected set { SetProperty(ref _currentPlayingIndex, value); }
        }

        public bool IsShuffleAndRepeatAvailable => !IsUnlimitedPlaylistSource;

        public bool IsShuffleModeRequested => _playerSettings.IsShuffleEnable;


        public bool IsShuffleModeEnabled => IsShuffleAndRepeatAvailable && IsShuffleModeRequested;

        public IObservable<IBufferedPlaylistItemsSource> GetBufferedItems()
        {
            return this.ObserveProperty(x => x.BufferedPlaylistItemsSource);
        }

        protected async ValueTask<IBufferedPlaylistItemsSource> Reset(IPlaylist playlist, IPlaylistSortOption sortOption)
        {
            if (CurrentPlaylist == playlist && (sortOption?.Equals(BufferedPlaylistItemsSource?.SortOption) ?? false))
            {
                return BufferedPlaylistItemsSource;
            }

            (BufferedPlaylistItemsSource as IDisposable)?.Dispose();
            BufferedPlaylistItemsSource = null;

            ClearCurrentContent();

            if (playlist is ISortablePlaylist shufflePlaylist)
            {
                BufferedShufflePlaylistItemsSource source = new BufferedShufflePlaylistItemsSource(shufflePlaylist, sortOption, _scheduler);
                BufferedPlaylistItemsSource = source;
                _maxItemsCount = shufflePlaylist.TotalCount;
                _shuffledIndexies = Enumerable.Range(0, shufflePlaylist.TotalCount).Shuffle().ToArray();
                IsUnlimitedPlaylistSource = false;

                _IndexUpdateTimingSubscriber = source.IndexUpdateTiming.Subscribe(_ => 
                {
                    _scheduler.Schedule(() => 
                    {
                        CurrentPlayingIndex = BufferedPlaylistItemsSource.IndexOf(CurrentPlaylistItem);
                    });
                });

                await source.GetAsync(0);
            }
            else if (playlist is IUnlimitedPlaylist unlimitedPlaylist)
            {
                BufferedPlaylistItemsSource = new BufferedPlaylistItemsSource(unlimitedPlaylist, sortOption);
                _maxItemsCount = null;
                _shuffledIndexies = null;
                IsUnlimitedPlaylistSource = true;
            }
            
            CurrentPlaylist = playlist;
            CurrentPlaylistSortOption = sortOption;

            return BufferedPlaylistItemsSource;
        }

        protected void SetCurrentContent(IVideoContent video, int index)
        {
            //Guard.IsNotNull(BufferedPlaylistItemsSource, nameof(BufferedPlaylistItemsSource));

            
            CurrentPlayingIndex = index;
            CurrentPlaylistItem = video;
        }

        protected void ClearCurrentContent()
        {
            CurrentPlayingIndex = -1;
            CurrentPlaylistItem = null;
        }

        private IVideoContent? _currentPlaylistItem;
        public IVideoContent? CurrentPlaylistItem
        {
            get { return _currentPlaylistItem; }
            private set { SetProperty(ref _currentPlaylistItem, value); }
        }


        protected void Clear()
        {
            _IndexUpdateTimingSubscriber?.Dispose();
            _IndexUpdateTimingSubscriber = null;
            (BufferedPlaylistItemsSource as IDisposable)?.Dispose();
            BufferedPlaylistItemsSource = null;
            _maxItemsCount = null;
            _shuffledIndexies = null;
            CurrentPlayingIndex = InvalidIndex;
        }

        public async Task<IVideoContent> GetNextItemAsync(CancellationToken ct = default)
        {
            using var _ = await _lock.LockAsync(ct);

            return await GetNextItemAsync_Internal(ct);
        }


        async ValueTask<IVideoContent?> GetNextItemAsync_Internal(CancellationToken ct)
        {
            if (CurrentPlayingIndex == InvalidIndex) { return null; }
            if (BufferedPlaylistItemsSource == null) { return null; }

            int candidateIndex = CurrentPlayingIndex + 1;
            if (IsShuffleModeEnabled)
            {
                Guard.IsNotNull(_maxItemsCount, nameof(_maxItemsCount));
                if (candidateIndex >= _maxItemsCount)
                {
                    return null;
                }

                int shuffledIndex = ToShuffledIndex(candidateIndex);
                return await BufferedPlaylistItemsSource.GetAsync(shuffledIndex, ct);
            }
            else
            {
                return await BufferedPlaylistItemsSource.GetAsync(candidateIndex, ct);
            }
        }


        public async Task<IVideoContent?> GetPreviewItemAsync(CancellationToken ct = default)
        {
            using var _ = await _lock.LockAsync(ct);

            return await GetPreviewItemAsync_Internal(ct);
        }
        async ValueTask<IVideoContent?> GetPreviewItemAsync_Internal(CancellationToken ct)
        {
            if (CurrentPlayingIndex == InvalidIndex) { return null; }
            if (BufferedPlaylistItemsSource == null) { return null; }

            int candidateIndex = CurrentPlayingIndex - 1;
            if (candidateIndex < 0)
            {
                return null;
            }

            if (IsShuffleModeEnabled)
            {
                int shuffledIndex = ToShuffledIndex(candidateIndex);
                return await BufferedPlaylistItemsSource.GetAsync(shuffledIndex, ct);
            }
            else
            {
                return await BufferedPlaylistItemsSource.GetAsync(candidateIndex, ct);
            }
        }

        int ToShuffledIndex(int index)
        {
            Guard.IsNotNull(_shuffledIndexies, nameof(_shuffledIndexies));

            return _shuffledIndexies[index];
        }

        public IObservable<bool> GetCanGoNextOrPreviewObservable()
        {
            return Observable.Merge(
                this.ObserveProperty(x => x.IsShuffleModeRequested).ToUnit(),
                this.ObserveProperty(x => x.IsUnlimitedPlaylistSource).ToUnit(),
                this.ObserveProperty(x => x.CurrentPlayingIndex).ToUnit(),
                this.ObserveProperty(x => x.CurrentPlaylist).SelectMany(x => x is INotifyCollectionChanged ncc ? ncc.CollectionChangedAsObservable().ToUnit() : Observable.Empty<Unit>())
                )
                .Select(x => IsUnlimitedPlaylistSource && CurrentPlayingIndex >= 0);
        }

        public async Task<bool> CanGoNextAsync(CancellationToken ct = default)
        {
            using var _ = await _lock.LockAsync(ct);
            return await CanGoNextAsync_Internal(ct);
        }

        protected async ValueTask<bool> CanGoNextAsync_Internal(CancellationToken ct)
        {
            return await GetNextItemAsync_Internal(ct) != null;
        }

        public async Task<bool> CanGoPreviewAsync(CancellationToken ct = default)
        {
            using var _ = await _lock.LockAsync(ct);
            return await CanGoPreviewAsync_Internal(ct);
        }

        protected  async ValueTask<bool> CanGoPreviewAsync_Internal(CancellationToken ct = default)
        {
            return await GetPreviewItemAsync_Internal(ct) != null;
        }


        Helpers.AsyncLock _lock = new ();
        public async Task<bool> GoNextAsync(CancellationToken ct = default)
        {
            using var _ = await _lock.LockAsync(ct);

            if (CurrentPlayingIndex == InvalidIndex) { return false; }
            if (BufferedPlaylistItemsSource == null) { return false; }

            var nextItem = await GetNextItemAsync_Internal(ct);
            if (nextItem != null)
            {
                await PlayVideoOnSamePlaylistAsync_Internal(nextItem);
                SetCurrentContent(nextItem, BufferedPlaylistItemsSource.IndexOf(nextItem));
                return true;
            }
            else
            {
                return false;
            }
        }

        public async Task<bool> GoPreviewAsync(CancellationToken ct = default)
        {
            using var _ = await _lock.LockAsync(ct);

            if (CurrentPlayingIndex == InvalidIndex) { return false; }
            if (BufferedPlaylistItemsSource == null) { return false; }

            var prevItem = await GetPreviewItemAsync_Internal(ct);
            if (prevItem != null)
            {
                await PlayVideoOnSamePlaylistAsync_Internal(prevItem);
                SetCurrentContent(prevItem, BufferedPlaylistItemsSource.IndexOf(prevItem));
                return true;
            }
            else
            {
                return false;
            }        
        }

        protected abstract Task PlayVideoOnSamePlaylistAsync_Internal(IVideoContent item, TimeSpan? startPosition = null);


        public virtual void Dispose()
        {
            _IndexUpdateTimingSubscriber?.Dispose();
            _IsShuffleEnableSubscriber?.Dispose();
        }
    }



    public sealed class HohoemaPlaylistPlayer : PlaylistPlayer
    {
        private readonly IMessenger _messenger;
        private readonly MediaPlayer _mediaPlayer;
        private readonly VideoStreamingOriginOrchestrator _videoStreamingOriginOrchestrator;
        private readonly PlayerSettings _playerSettings;
        private readonly MediaPlayerSoundVolumeManager _soundVolumeManager;
        private readonly RestoreNavigationManager _restoreNavigationManager;
        private readonly SystemMediaTransportControls _smtc;
        private readonly DispatcherQueue _dispatcherQueue;

        public HohoemaPlaylistPlayer(
            IScheduler scheduler,
            IMessenger messenger,
            MediaPlayer mediaPlayer,
            VideoStreamingOriginOrchestrator videoStreamingOriginOrchestrator,
            PlayerSettings playerSettings,
            MediaPlayerSoundVolumeManager soundVolumeManager,
            RestoreNavigationManager restoreNavigationManager
            )
            : base(playerSettings, scheduler)
        {
            _messenger = messenger;
            _mediaPlayer = mediaPlayer;
            _videoStreamingOriginOrchestrator = videoStreamingOriginOrchestrator;
            _playerSettings = playerSettings;
            _soundVolumeManager = soundVolumeManager;
            _restoreNavigationManager = restoreNavigationManager;
            _smtc = SystemMediaTransportControls.GetForCurrentView();
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

            _saveTimer = _dispatcherQueue.CreateTimer();
            _saveTimer.Interval = TimeSpan.FromSeconds(5);
            _saveTimer.IsRepeating = true;
            _saveTimer.Tick += (s, _) =>
            {
                if (CurrentPlaylistItem == null)
                {
                    _saveTimer.Stop();
                    return;
                }

                //if (PrimaryViewPlayerManager.DisplayMode == PrimaryPlayerDisplayMode.Close) { return; }
                if (_mediaPlayer.PlaybackSession?.PlaybackState is not MediaPlaybackState.Playing) { return; }

                _restoreNavigationManager.SetCurrentPlayerEntry(
                        new PlayerEntry()
                        {
                            ContentId = CurrentPlaylistItem.VideoId,
                            Position = _mediaPlayer.PlaybackSession.Position,
                            PlaylistId = CurrentPlaylistId?.Id,
                            PlaylistOrigin = CurrentPlaylistId?.Origin
                        });
            };


        }

        public override void Dispose()
        {
            base.Dispose();

            _videoSessionDisposable?.Dispose();
        }

        private readonly DispatcherQueueTimer _saveTimer;

        private void StartStateSavingTimer()
        {
            _saveTimer.Start();
        }

        private void StopStateSavingTimer()
        {
            _saveTimer.Stop();
        }


        public PlaylistId? CurrentPlaylistId => CurrentPlaylist?.PlaylistId;


        public PlayingOrchestrateResult CurrentPlayingSession { get; private set; }

        IDisposable _videoSessionDisposable;




        private NicoVideoQualityEntity _currentQuality;
        public NicoVideoQualityEntity CurrentQuality
        {
            get { return _currentQuality; }
            private set { SetProperty(ref _currentQuality, value); }
        }


        private bool _nowPlayingWithCache;
        public bool NowPlayingWithCache
        {
            get { return _nowPlayingWithCache; }
            private set { SetProperty(ref _nowPlayingWithCache, value); }
        }

        public IReadOnlyCollection<NicoVideoQualityEntity> AvailableQualities => CurrentPlayingSession?.VideoSessionProvider?.AvailableQualities;


        public async ValueTask ClearAsync()
        {
            await _dispatcherQueue.EnqueueAsync(() => 
            {
                base.Clear();

                StopPlaybackMedia();
                ClearPlaylist();
            });
        }

        public TimeSpan? GetCurrentPlaybackPosition()
        {
            return _mediaPlayer.PlaybackSession?.Position;
        }

        public bool CanPlayQuality(NicoVideoQuality quality)
        {
            var qualityEntity = AvailableQualities.FirstOrDefault(x => x.Quality == quality);
            return qualityEntity?.IsAvailable == true;
        }


        public async Task ChangeQualityAsync(NicoVideoQuality quality)
        {
            if (CurrentPlaylistItem == null) { return; }

            if (CurrentPlayingSession?.IsSuccess is null or false)
            {
                return;
            }

            var currentPosition = GetCurrentPlaybackPosition();

            _videoSessionDisposable?.Dispose();
            _videoSessionDisposable = null;

            var quelityEntity = AvailableQualities.First(x => x.Quality == quality);

            if (quelityEntity.IsAvailable is false)
            {
                throw new HohoemaExpception("unavailable video quality : " + quality);
            }

            var videoSession = await CurrentPlayingSession.VideoSessionProvider.CreateVideoSessionAsync(quality);

            _videoSessionDisposable = videoSession;
            await videoSession.StartPlayback(_mediaPlayer, currentPosition ?? TimeSpan.Zero);

            CurrentQuality = quelityEntity;

            _playerSettings.DefaultVideoQuality = quality;
            Guard.IsNotNull(_mediaPlayer.PlaybackSession, nameof(_mediaPlayer.PlaybackSession));
        }

        private void StopPlaybackMedia()
        {
            var prevSource = _mediaPlayer.Source;

            var endPosition = _mediaPlayer.PlaybackSession.Position;
            var videoId = CurrentPlaylistItem?.VideoId;

            _videoSessionDisposable?.Dispose();
            _videoSessionDisposable = null;
            CurrentPlayingSession = null;
            _mediaPlayer.Pause();
            _mediaPlayer.Source = null;
            ClearCurrentContent();

            if (prevSource != null && videoId.HasValue)
            {
                // メディア停止メッセージを飛ばす
                _messenger.Send(new PlaybackStopedMessage(new(this, CurrentPlaylistId, videoId.Value, endPosition, PlaybackStopReason.FromUser)));
            }

            _soundVolumeManager.LoudnessCorrectionValue = 1.0;

            _dispatcherQueue.TryEnqueue(() => 
            {
                _smtc.IsEnabled = false;
                _smtc.PlaybackStatus = MediaPlaybackStatus.Closed;
                _smtc.ButtonPressed -= _smtc_ButtonPressed;
                _smtc.DisplayUpdater.ClearAll();
                _smtc.DisplayUpdater.Update();
            });

            StopStateSavingTimer();
        }

        public async Task ReopenAsync(TimeSpan? position = null)
        {
            if (CurrentPlaylistItem != null)
            {
                if (position == null)
                {
                    position = _mediaPlayer.PlaybackSession?.Position;
                }

                if (false == await UpdatePlayingMediaAsync(CurrentPlaylistItem, position))
                {
                    return;
                }
            }
        }
        public async Task<bool> PlayWithoutPlaylistAsync(IVideoContent video, TimeSpan? initialPosition)
        {
            await PlayVideoOnSamePlaylistAsync_Internal(video, initialPosition);
            SetCurrentContent(video, 0);
            return true;
        }

        public async Task<bool> PlayOnCurrentPlaylistAsync(IVideoContent video)
        {
            await PlayVideoOnSamePlaylistAsync_Internal(video);
            SetCurrentContent(video, BufferedPlaylistItemsSource.IndexOf(video));
            return true;
        }


        public async Task<bool> PlayAsync(IPlaylist playlist, IPlaylistSortOption sortOption)
        {
            Guard.IsNotNull(playlist, nameof(playlist));
            Guard.IsNotNull(sortOption, nameof(sortOption));

            return await _dispatcherQueue.EnqueueAsync(async () =>
            {
                StopPlaybackMedia();
                try
                {
                    var bufferItemsSource = await UpdatePlaylistItemsSourceAsync(playlist, sortOption);
                    var firstItem = await bufferItemsSource.GetAsync(0);
                    if (firstItem == null || false == await UpdatePlayingMediaAsync(firstItem, null))
                    {
                        return false;
                    }

                    SetCurrentContent(firstItem, 0);

                    return true;
                }
                catch
                {
                    Clear();
                    ClearPlaylist();
                    ClearCurrentContent();
                    return false;
                }
            });
        }

        public async Task<bool> PlayAsync(ISortablePlaylist playlist, IPlaylistSortOption sortOption, IVideoContent item, TimeSpan? startPosition = null)
        {
            Guard.IsNotNull(playlist, nameof(playlist));
            Guard.IsNotNull(sortOption, nameof(sortOption));
            Guard.IsNotNull(item, nameof(item));
            Guard.IsFalse(item.VideoId == default(VideoId), "Not contain playable VideoId or PlaylistId");

            if (startPosition == null
                && CurrentPlaylistItem?.VideoId == item.VideoId)
            {
                startPosition = _mediaPlayer.PlaybackSession?.Position;
            }

            return await _dispatcherQueue.EnqueueAsync(async () =>
            {
                StopPlaybackMedia();

                try
                {
                    var bufferedItems = await UpdatePlaylistItemsSourceAsync(playlist, sortOption);

                    bool result = await UpdatePlayingMediaAsync(item, startPosition);

                    var index = bufferedItems.IndexOf(item);

                    Guard.IsBetweenOrEqualTo(index, 0, 5000, nameof(index));
                    SetCurrentContent(item, index);

                    return result;
                }
                catch
                {
                    Clear();
                    ClearPlaylist();
                    ClearCurrentContent();

                    return false;
                }
            });
            
        }

        protected override async Task PlayVideoOnSamePlaylistAsync_Internal(IVideoContent item, TimeSpan? startPosition = null)
        {
            Guard.IsNotNull(item, nameof(item));
            //Guard.IsNotNull(CurrentPlaylistId, nameof(CurrentPlaylistId));
            Guard.IsFalse(item.VideoId == default(VideoId), "Not contain playable VideoId or PlaylistId");

            if (startPosition == null
                && CurrentPlaylistItem?.VideoId == item.VideoId)
            {
                startPosition = _mediaPlayer.PlaybackSession?.Position;
            }

            StopPlaybackMedia();

            if (false == await UpdatePlayingMediaAsync(item, startPosition))
            {
                return;
            }
        }

        private async Task<bool> UpdatePlayingMediaAsync(IVideoContent item, TimeSpan? startPosition)
        {
            Guard.IsNotNull(item, nameof(item));

            try
            {
                var result = await _videoStreamingOriginOrchestrator.CreatePlayingOrchestrateResultAsync(item.VideoId);
                CurrentPlayingSession = result;
                if (!result.IsSuccess)
                {
                    _messenger.Send(new PlaybackFailedMessage(new(this, CurrentPlaylistId, item.VideoId, result.PlayingOrchestrateFailedReason)));
                    return false;
                }

                var qualityEntity = AvailableQualities.FirstOrDefault(x => x.Quality == _playerSettings.DefaultVideoQuality);
                if (qualityEntity?.IsAvailable is null or false)
                {
                    qualityEntity = AvailableQualities.SkipWhile(x => !x.IsAvailable).First();
                }
                
                var videoSession = await result.VideoSessionProvider.CreateVideoSessionAsync(qualityEntity.Quality);

                _videoSessionDisposable = videoSession;
                await videoSession.StartPlayback(_mediaPlayer, startPosition ?? TimeSpan.Zero);

                CurrentQuality = AvailableQualities.First(x => x.Quality == videoSession.Quality);
                Guard.IsNotNull(_mediaPlayer.PlaybackSession, nameof(_mediaPlayer.PlaybackSession));


                OnPropertyChanged(nameof(AvailableQualities));

                NowPlayingWithCache = videoSession is CachedVideoStreamingSession;
                _soundVolumeManager.LoudnessCorrectionValue = CurrentPlayingSession.VideoDetails.LoudnessCorrectionValue;

                // メディア再生成功時のメッセージを飛ばす
                _messenger.Send(new PlaybackStartedMessage(new(this, CurrentPlaylistId, item.VideoId, videoSession.Quality, _mediaPlayer.PlaybackSession)));

                _mediaPlayer.PlaybackSession.PlaybackStateChanged += PlaybackSession_PlaybackStateChanged;

                _mediaPlayer.CommandManager.IsEnabled = false;

                await _dispatcherQueue.EnqueueAsync(async () => 
                {
                    _smtc.IsEnabled = true;
                    _smtc.IsPauseEnabled = true;
                    _smtc.IsPlayEnabled = true;
                    _smtc.IsStopEnabled = true;
                    _smtc.IsFastForwardEnabled = true;
                    _smtc.IsNextEnabled = await CanGoNextAsync_Internal(default);
                    _smtc.IsPreviousEnabled = await CanGoPreviewAsync_Internal(default);
                    _smtc.DisplayUpdater.ClearAll();
                    _smtc.DisplayUpdater.Type = MediaPlaybackType.Video;
                    _smtc.DisplayUpdater.VideoProperties.Title = CurrentPlayingSession.VideoDetails.Title;
                    _smtc.DisplayUpdater.VideoProperties.Subtitle = CurrentPlayingSession.VideoDetails.ProviderName ?? String.Empty; // 投稿者退会済みの場合nullになるのでカバー
                    _smtc.DisplayUpdater.Thumbnail = RandomAccessStreamReference.CreateFromUri(new Uri(CurrentPlayingSession.VideoDetails.ThumbnailUrl));
                    _smtc.DisplayUpdater.Update();
                });

                _smtc.ButtonPressed -= _smtc_ButtonPressed; 
                _smtc.ButtonPressed += _smtc_ButtonPressed;

                StartStateSavingTimer();

                return true;
            }
            catch (Exception ex)
            {
                StopPlaybackMedia();
                _messenger.Send(new PlaybackFailedMessage(new(this, CurrentPlaylistId, item.VideoId, PlayingOrchestrateFailedReason.Unknown)));
                throw;
            }            
        }

        private void PlaybackSession_PlaybackStateChanged(MediaPlaybackSession sender, object args)
        {
            _smtc.PlaybackStatus = sender.PlaybackState switch
            {
                MediaPlaybackState.None => MediaPlaybackStatus.Closed,
                MediaPlaybackState.Opening => MediaPlaybackStatus.Changing,
                MediaPlaybackState.Buffering => MediaPlaybackStatus.Changing,
                MediaPlaybackState.Playing => MediaPlaybackStatus.Playing,
                MediaPlaybackState.Paused => MediaPlaybackStatus.Paused,
                _ => throw new NotSupportedException(),
            };
            /*
            if (sender.PlaybackState == MediaPlaybackState.Paused)
            {
                _smtc.IsPlayEnabled = true;
                _smtc.IsPauseEnabled = false;
                _smtc.DisplayUpdater.Update();
            }
            else if (sender.PlaybackState == MediaPlaybackState.Playing)
            {
                _smtc.IsPlayEnabled = false;
                _smtc.IsPauseEnabled = true;
                _smtc.DisplayUpdater.Update();
            }
            */
        }

        private void _smtc_ButtonPressed(SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs args)
        {
            _dispatcherQueue.TryEnqueue(() => 
            {
                switch (args.Button)
                {
                    case SystemMediaTransportControlsButton.FastForward:
                        _mediaPlayer.PlaybackSession.Position += TimeSpan.FromSeconds(30);
                        break;
                    case SystemMediaTransportControlsButton.Next:
                        _ = GoNextAsync();
                        break;
                    case SystemMediaTransportControlsButton.Previous:
                        _ = GoPreviewAsync();
                        break;
                    case SystemMediaTransportControlsButton.Stop:
                        _ = ClearAsync();
                        break;
                    case SystemMediaTransportControlsButton.Pause:
                        _mediaPlayer.Pause();
                        break;
                    case SystemMediaTransportControlsButton.Play:
                        _mediaPlayer.Play();
                        break;
                }
            });
        }

        private void ClearPlaylist()
        {
            bool isCleared = CurrentPlaylist != null;
            CurrentPlaylist = null;
            if (isCleared)
            {
                _messenger.Send(new PlayingPlaylistChangedMessage(new(this, null)));
            }
        }

        /*
        private async ValueTask UpdatePlaylistItemsSourceAsync(PlaylistId playlistId, string serializedSortOptions, IVideoContent currentItem)
        {
            Guard.IsNotNull(playlistId, nameof(playlistId));

            if (CurrentPlaylistId == playlistId) { return; }

            IPlaylist newPlaylist = null;
            try
            {
                newPlaylist = await _playlistSourceManager.ResolveItemsSource(playlistId, serializedSortOptions);                
            }
            catch
            {
                _messenger.Send(new ResolvePlaylistFailedMessage(new(playlistId)));
                throw;
            }

            await UpdatePlaylistItemsSourceAsync(newPlaylist, currentItem);
            // プレイリスト更新メッセージを飛ばす
            _messenger.Send(new PlayingPlaylistChangedMessage(new(CurrentPlaylist)));
        }
        */

        private async ValueTask<IBufferedPlaylistItemsSource> UpdatePlaylistItemsSourceAsync(IPlaylist playlist, IPlaylistSortOption sortOption)
        {
            try
            {
                var source = await Reset(playlist, sortOption);                

                // プレイリスト更新メッセージを飛ばす
                _messenger.Send(new PlayingPlaylistChangedMessage(new(this, CurrentPlaylist)));

                return source;
            }
            catch
            {
                CurrentPlaylist = null;
                _messenger.Send(new ResolvePlaylistFailedMessage(new(this, playlist.PlaylistId)));
                throw;
            }
        }

    }
}
