﻿using Hohoema.Models.Domain.Niconico.Player;
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Domain.PageNavigation;
using Hohoema.Models.Domain.Player;
using Hohoema.Models.Domain.Player.Video;
using Hohoema.Models.Domain.VideoCache;
using Hohoema.Models.Helpers;
using Hohoema.Models.Infrastructure;
using Microsoft.Toolkit.Collections;
using Microsoft.Toolkit.Diagnostics;
using Microsoft.Toolkit.Mvvm.Messaging;
using Microsoft.Toolkit.Mvvm.Messaging.Messages;
using Microsoft.Toolkit.Uwp;
using NiconicoToolkit.Video;
using Prism.Mvvm;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive.Concurrency;
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
    public record PlaybackStartedMessageData(PlaylistId PlaylistId, VideoId VideoId, NicoVideoQuality VideoQuality, MediaPlaybackSession Session);

    public sealed class PlaybackStartedMessage : ValueChangedMessage<PlaybackStartedMessageData>
    {
        public PlaybackStartedMessage(PlaybackStartedMessageData value) : base(value)
        {
        }
    }


    public record PlaybackFailedMessageData(PlaylistId PlaylistId, VideoId VideoId, Exception Exception);

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

    public record PlaybackStopedMessageData(PlaylistId PlaylistId, VideoId VideoId, TimeSpan EndPosition, PlaybackStopReason StopReason);

    public sealed class PlaybackStopedMessage : ValueChangedMessage<PlaybackStopedMessageData>
    {
        public PlaybackStopedMessage(PlaybackStopedMessageData value) : base(value)
        {
        }
    }

    public record PlayingPlaylistChangedMessageData(IPlaylist? PlaylistItemsSource);

    public sealed class PlayingPlaylistChangedMessage : ValueChangedMessage<PlayingPlaylistChangedMessageData>
    {
        public PlayingPlaylistChangedMessage(PlayingPlaylistChangedMessageData value) : base(value)
        {
        }
    }

    public record ResolvePlaylistFailedMessageData(PlaylistId PlaylistId);

    public sealed class ResolvePlaylistFailedMessage : ValueChangedMessage<ResolvePlaylistFailedMessageData>
    {
        public ResolvePlaylistFailedMessage(ResolvePlaylistFailedMessageData value) : base(value)
        {
        }
    }


    public interface IBufferedPlaylistItemsSource : IIncrementalSource<IVideoContent>
    {
        int BufferLength { get; }

        int IndexOf(IVideoContent item);

        ValueTask<IVideoContent> GetAsync(int index, CancellationToken ct = default);
    }


    public sealed class BufferedPlaylistItemsSource : ReadOnlyObservableCollection<IVideoContent>, IBufferedPlaylistItemsSource
    {
        public const int MaxBufferSize = 2000;
        private readonly IPlaylist _playlistItemsSource;

        public ReadOnlyObservableCollection<IVideoContent> BufferedItems { get; }

        public int BufferLength => this.Count;


        public BufferedPlaylistItemsSource(IPlaylist playlistItemsSource)
            : base(new ObservableCollection<IVideoContent>())
        {
            _playlistItemsSource = playlistItemsSource;
        }

        public int OneTimeLoadingItemsCount = 30;


        public async ValueTask<IVideoContent?> GetAsync(int index, CancellationToken ct = default)
        {
            if (index < 0) { return null; }

            var page = index / OneTimeLoadingItemsCount;
            await GetPagedItemsAsync(page, OneTimeLoadingItemsCount, ct);

            if (index >= this.Items.Count) { return null; }

            return this.Items[index];
        }

        public async Task<IEnumerable<IVideoContent>> GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken ct = default)
        {
            pageSize = OneTimeLoadingItemsCount;
            int head = pageIndex * pageSize;
            var cached = this.Items.Skip(head).Take(pageSize);
            if (cached.Any() && cached.All(x => x is not null))
            {
                head += pageSize;
                return this.Items.Skip(head).Take(pageSize);
            }

            var items = await _playlistItemsSource.GetPagedItemsAsync(pageIndex, pageSize, ct);
            foreach (var item in items)
            {
                this.Items.Add(item);
            }

            return this.Items.Skip(head).Take(pageSize);
        }
    }

    public sealed class BufferedShufflePlaylistItemsSource : ReadOnlyObservableCollection<IVideoContent>, IBufferedPlaylistItemsSource
    {
        public const int MaxBufferSize = 2000;
        public const int DefaultBufferSize = 500;

        private readonly IUserManagedPlaylist _playlistItemsSource;

        public int BufferLength => this.Count;

        public BufferedShufflePlaylistItemsSource(IUserManagedPlaylist shufflePlaylist)
            : base(new ObservableCollection<IVideoContent>(new IVideoContent[shufflePlaylist.TotalCount]))
        {
            _playlistItemsSource = shufflePlaylist;

            // shufflePlaylist.AddedItem 等をハンドルする
            // 可変配列として対応したいのでListを使用したほうが良さそう
            // 
        }

        public int OneTimeLoadingItemsCount = 30;

        public PlaylistId PlaylistId => _playlistItemsSource.PlaylistId;

        public async ValueTask<IVideoContent> GetAsync(int index, CancellationToken ct = default)
        {
            if (index < 0) { return null; }
            if (index >= this.Items.Count) { return null; }

            var indexInsidePage = index / OneTimeLoadingItemsCount;
            await GetPagedItemsAsync(indexInsidePage, OneTimeLoadingItemsCount, ct);

            return this.Items[index];
        }

        public async Task<IEnumerable<IVideoContent>> GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken ct = default)
        {
            var start = pageIndex * OneTimeLoadingItemsCount;
            int current = pageIndex * OneTimeLoadingItemsCount;
            if (this.Items.Any() && this.Items.Skip(start).Take(OneTimeLoadingItemsCount).All(x => x is not null))
            {
                current += OneTimeLoadingItemsCount;
                return this.Items.Skip(start).Take(OneTimeLoadingItemsCount);
            }

            var items = await _playlistItemsSource.GetPagedItemsAsync(pageIndex, OneTimeLoadingItemsCount, ct);
            foreach (var item in items)
            {
                this.Items[current++] = item;
            }

            return this.Items.Skip(start).Take(OneTimeLoadingItemsCount);
        }

    }

    public abstract class PlaylistPlayer : BindableBase
    {
        private const int InvalidIndex = -1;
        private readonly IScheduler _scheduler;
        private IBufferedPlaylistItemsSource? _bufferedPlaylistItemsSource;

        public PlaylistPlayer(IScheduler scheduler)
        {
            _scheduler = scheduler;
        }

        private int[] _shuffledIndexies;
        private int? _maxItemsCount;        

        private IPlaylist _currentPlaylist;
        public IPlaylist CurrentPlaylist
        {
            get { return _currentPlaylist; }
            set { SetProperty(ref _currentPlaylist, value); }
        }

        private bool _isUnlimitedPlaylistSource;
        public bool IsUnlimitedPlaylistSource
        {
            get { return _isUnlimitedPlaylistSource; }
            set
            {
                if (SetProperty(ref _isUnlimitedPlaylistSource, value))
                {
                    RaisePropertyChanged(nameof(IsShuffleAndRepeatAvairable));
                    RaisePropertyChanged(nameof(IsShuffleModeEnabled));
                }
            }
        }

        private int _currentPlayingIndex;
        public int CurrentPlayingIndex
        {
            get { return _currentPlayingIndex; }
            protected set { SetProperty(ref _currentPlayingIndex, value); }
        }
        public bool IsShuffleAndRepeatAvairable => !IsUnlimitedPlaylistSource;

        private bool _isShuffleModeRequested;
        public bool IsShuffleModeRequested
        {
            get { return _isShuffleModeRequested; }
            set 
            {
                if (SetProperty(ref _isShuffleModeRequested, value))
                {
                    RaisePropertyChanged(nameof(IsShuffleModeEnabled));
                }
            }
        }

        public bool IsShuffleModeEnabled => IsShuffleAndRepeatAvairable && IsShuffleModeRequested;

        public ReadOnlyObservableCollection<IVideoContent> GetBufferedItemsAsSpan()
        {
            return _bufferedPlaylistItemsSource as ReadOnlyObservableCollection<IVideoContent>;
        }

        protected IBufferedPlaylistItemsSource Reset(IPlaylist playlistItemsSource)
        {
            (_bufferedPlaylistItemsSource as IDisposable)?.Dispose();
            _bufferedPlaylistItemsSource = null;

            ClearCurrentContent();

            if (playlistItemsSource is IUserManagedPlaylist shufflePlaylist)
            {
                _bufferedPlaylistItemsSource = new BufferedShufflePlaylistItemsSource(shufflePlaylist);
                _maxItemsCount = shufflePlaylist.TotalCount;
                _shuffledIndexies = Enumerable.Range(0, shufflePlaylist.TotalCount).Shuffle().ToArray();
                IsUnlimitedPlaylistSource = false;
            }
            else
            {
                _bufferedPlaylistItemsSource = new BufferedPlaylistItemsSource(playlistItemsSource);
                _maxItemsCount = null;
                _shuffledIndexies = null;
                IsUnlimitedPlaylistSource = true;
            }

            return _bufferedPlaylistItemsSource;
        }

        protected void SetCurrentContent(IVideoContent video, int index)
        {
            Guard.IsNotNull(_bufferedPlaylistItemsSource, nameof(_bufferedPlaylistItemsSource));

            
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
            (_bufferedPlaylistItemsSource as IDisposable)?.Dispose();
            _bufferedPlaylistItemsSource = null;
            _maxItemsCount = null;
            _shuffledIndexies = null;
            CurrentPlayingIndex = InvalidIndex;
        }


        public async ValueTask<IVideoContent?> GetNextItemAsync(CancellationToken ct = default)
        {
            if (CurrentPlayingIndex == InvalidIndex) { return null; }
            if (_bufferedPlaylistItemsSource == null) { return null; }

            int index = IndexTranformWithCurrentPlaylistMode(CurrentPlayingIndex + 1);
            return await _bufferedPlaylistItemsSource.GetAsync(index, ct);
        }

        public async ValueTask<IVideoContent?> GetPreviewItemAsync(CancellationToken ct = default)
        {
            if (CurrentPlayingIndex == InvalidIndex) { return null; }
            if (_bufferedPlaylistItemsSource == null) { return null; }

            int index = IndexTranformWithCurrentPlaylistMode(CurrentPlayingIndex - 1);
            return await _bufferedPlaylistItemsSource.GetAsync(index, ct);
        }

        int IndexTranformWithCurrentPlaylistMode(int index)
        {
            int ToShuffledIndex(int index)
            {
                Guard.IsNotNull(_shuffledIndexies, nameof(_shuffledIndexies));

                return _shuffledIndexies[index];
            }
            
            return IsShuffleModeEnabled ? ToShuffledIndex(index) : index;
        }

        public async ValueTask<bool> CanGoNextAsync(CancellationToken ct = default)
        {
            return await GetNextItemAsync(ct) != null;
        }

        public async ValueTask<bool> CanGoPreviewAsync(CancellationToken ct = default)
        {
            return await GetPreviewItemAsync(ct) != null;
        }


        public async ValueTask<bool> GoNextAsync(CancellationToken ct = default)
        {
            if (CurrentPlayingIndex == InvalidIndex) { return false; }
            if (_bufferedPlaylistItemsSource == null) { return false; }

            int index = IndexTranformWithCurrentPlaylistMode(CurrentPlayingIndex + 1);
            if (index >= (_maxItemsCount ?? 5000)) { return false; }
            Guard.IsBetweenOrEqualTo(index, 0, _maxItemsCount ?? 5000, nameof(index));
            var item = await _bufferedPlaylistItemsSource.GetAsync(index, ct);
            if (item != null)
            {
                await PlayVideoOnSamePlaylistAsync_Internal(item);
                SetCurrentContent(item, index);
                return true;
            }
            else
            {
                return false;
            }
        }

        public async ValueTask<bool> GoPreviewAsync(CancellationToken ct = default)
        {
            if (CurrentPlayingIndex == InvalidIndex) { return false; }
            if (_bufferedPlaylistItemsSource == null) { return false; }

            int index = IndexTranformWithCurrentPlaylistMode(CurrentPlayingIndex - 1);
            if (index < 0) { return false; }
            Guard.IsBetweenOrEqualTo(index, 0, _maxItemsCount ?? 5000, nameof(index));
            var item = await _bufferedPlaylistItemsSource.GetAsync(index, ct);
            if (item != null)
            {
                await PlayVideoOnSamePlaylistAsync_Internal(item);
                SetCurrentContent(item, index);
                return true;
            }
            else
            {
                return false;
            }        
        }

        protected abstract Task PlayVideoOnSamePlaylistAsync_Internal(IVideoContent item, TimeSpan? startPosition = null);

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
            : base(scheduler)
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


        private readonly DispatcherQueueTimer _saveTimer;

        private void StartStateSavingTimer()
        {
            _saveTimer.Start();
        }

        private void StopStateSavingTimer()
        {
            _saveTimer.Stop();
        }


        public PlaylistId? CurrentPlaylistId => _itemsSource?.PlaylistId;

        private IPlaylist? _itemsSource;

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
            base.Clear();

            await _dispatcherQueue.EnqueueAsync(() => 
            {
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
            _mediaPlayer.Source = null;
            ClearCurrentContent();

            if (prevSource != null && videoId.HasValue)
            {
                // メディア停止メッセージを飛ばす
                _messenger.Send(new PlaybackStopedMessage(new(CurrentPlaylistId, videoId.Value, endPosition, PlaybackStopReason.FromUser)));
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
            if (CurrentPlaylist != null 
                && CurrentPlaylistItem != null)
            {
                if (position == null)
                {
                    position = _mediaPlayer.PlaybackSession?.Position;
                }

                await UpdatePlayingMediaAsync(CurrentPlaylistItem, position);
            }
        }

        public async Task PlayAsync(IPlaylist playlist)
        {
            StopPlaybackMedia();

            var bufferItemsSource = await UpdatePlaylistItemsSourceAsync(playlist);

            var firstItem = await bufferItemsSource.GetAsync(0);

            await UpdatePlayingMediaAsync(firstItem, null);

            await _dispatcherQueue.EnqueueAsync(() => SetCurrentContent(firstItem, 0));
        }

        public async Task PlayAsync(IPlaylist playlist, IVideoContent item, TimeSpan? startPosition = null)
        {
            Guard.IsNotNull(item, nameof(item));
            Guard.IsFalse(item.VideoId == default(VideoId), "Not contain playable VideoId or PlaylistId");

            var index = playlist.IndexOf(item);
            Guard.IsBetweenOrEqualTo(index, 0, 5000, nameof(index));

            if (startPosition == null
                && CurrentPlaylistItem?.VideoId == item.VideoId)
            {
                startPosition = _mediaPlayer.PlaybackSession?.Position;
            }

            StopPlaybackMedia();

            await UpdatePlaylistItemsSourceAsync(playlist);

            await UpdatePlayingMediaAsync(item, startPosition);

            await _dispatcherQueue.EnqueueAsync(() => SetCurrentContent(item, index));
            
        }

        protected override async Task PlayVideoOnSamePlaylistAsync_Internal(IVideoContent item, TimeSpan? startPosition = null)
        {
            Guard.IsNotNull(item, nameof(item));
            Guard.IsNotNull(CurrentPlaylistId, nameof(CurrentPlaylistId));
            Guard.IsFalse(item.VideoId == default(VideoId), "Not contain playable VideoId or PlaylistId");

            if (startPosition == null
                && CurrentPlaylistItem?.VideoId == item.VideoId)
            {
                startPosition = _mediaPlayer.PlaybackSession?.Position;
            }

            StopPlaybackMedia();

            await UpdatePlayingMediaAsync(item, startPosition);
        }

        private async Task UpdatePlayingMediaAsync(IVideoContent item, TimeSpan? startPosition)
        {
            Guard.IsNotNull(item, nameof(item));

            try
            {
                var result = await _videoStreamingOriginOrchestrator.CreatePlayingOrchestrateResultAsync(item.VideoId);
                CurrentPlayingSession = result;
                if (!result.IsSuccess)
                {
                    throw new HohoemaExpception("failed playing start.", result.Exception);
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


                RaisePropertyChanged(nameof(AvailableQualities));

                NowPlayingWithCache = videoSession is CachedVideoStreamingSession;
                _soundVolumeManager.LoudnessCorrectionValue = CurrentPlayingSession.VideoDetails.LoudnessCorrectionValue;

                // メディア再生成功時のメッセージを飛ばす
                _messenger.Send(new PlaybackStartedMessage(new(CurrentPlaylistId, item.VideoId, videoSession.Quality, _mediaPlayer.PlaybackSession)));

                _mediaPlayer.PlaybackSession.PlaybackStateChanged += PlaybackSession_PlaybackStateChanged;

                _mediaPlayer.CommandManager.IsEnabled = false;

                await _dispatcherQueue.EnqueueAsync(async () => 
                {
                    _smtc.IsEnabled = true;
                    _smtc.IsPauseEnabled = true;
                    _smtc.IsPlayEnabled = true;
                    _smtc.IsStopEnabled = true;
                    _smtc.IsFastForwardEnabled = true;
                    _smtc.IsNextEnabled = await CanGoNextAsync();
                    _smtc.IsPreviousEnabled = await CanGoPreviewAsync();
                    _smtc.DisplayUpdater.ClearAll();
                    _smtc.DisplayUpdater.Type = MediaPlaybackType.Video;
                    _smtc.DisplayUpdater.VideoProperties.Title = CurrentPlayingSession.VideoDetails.Title;
                    _smtc.DisplayUpdater.VideoProperties.Subtitle = CurrentPlayingSession.VideoDetails.ProviderName;
                    _smtc.DisplayUpdater.Thumbnail = RandomAccessStreamReference.CreateFromUri(new Uri(CurrentPlayingSession.VideoDetails.ThumbnailUrl));
                    _smtc.DisplayUpdater.Update();
                });

                _smtc.ButtonPressed -= _smtc_ButtonPressed; 
                _smtc.ButtonPressed += _smtc_ButtonPressed;

                StartStateSavingTimer();
            }
            catch (Exception ex)
            {
                StopPlaybackMedia();
                _messenger.Send(new PlaybackFailedMessage(new(CurrentPlaylistId, item.VideoId, ex)));
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
            bool isCleared = _itemsSource != null;
            _itemsSource = null;
            if (isCleared)
            {
                _messenger.Send(new PlayingPlaylistChangedMessage(new(null)));
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
            _messenger.Send(new PlayingPlaylistChangedMessage(new(_itemsSource)));
        }
        */

        private async ValueTask<IBufferedPlaylistItemsSource> UpdatePlaylistItemsSourceAsync(IPlaylist playlist)
        {
            try
            {
                var source = Reset(playlist);
                _itemsSource = playlist;

                // プレイリスト更新メッセージを飛ばす
                _messenger.Send(new PlayingPlaylistChangedMessage(new(_itemsSource)));

                return source;
            }
            catch
            {
                _itemsSource = null;
                _messenger.Send(new ResolvePlaylistFailedMessage(new(playlist.PlaylistId)));
                throw;
            }
        }

    }
}
