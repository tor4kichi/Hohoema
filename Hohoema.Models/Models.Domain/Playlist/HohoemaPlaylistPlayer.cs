using Hohoema.Models.Domain.Niconico.Player;
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Domain.Player;
using Hohoema.Models.Domain.Player.Video;
using Hohoema.Models.Domain.VideoCache;
using Hohoema.Models.Helpers;
using Hohoema.Models.Infrastructure;
using Microsoft.Toolkit.Diagnostics;
using Microsoft.Toolkit.Mvvm.Messaging;
using Microsoft.Toolkit.Mvvm.Messaging.Messages;
using Microsoft.Toolkit.Uwp;
using NiconicoToolkit.Video;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Media.Playback;
using static Hohoema.Models.Domain.Niconico.Player.VideoStreamingOriginOrchestrator;

namespace Hohoema.Models.Domain.Playlist
{
    public record PlaybackStartedMessageData(PlaylistItem Item, NicoVideoQuality VideoQuality, MediaPlaybackSession Session);

    public sealed class PlaybackStartedMessage : ValueChangedMessage<PlaybackStartedMessageData>
    {
        public PlaybackStartedMessage(PlaybackStartedMessageData value) : base(value)
        {
        }
    }


    public record PlaybackFailedMessageData(PlaylistItem Item, Exception Exception);

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

    public record PlaybackStopedMessageData(VideoId VideoId, TimeSpan EndPosition, PlaybackStopReason StopReason);

    public sealed class PlaybackStopedMessage : ValueChangedMessage<PlaybackStopedMessageData>
    {
        public PlaybackStopedMessage(PlaybackStopedMessageData value) : base(value)
        {
        }
    }

    public record PlayingPlaylistChangedMessageData(IPlaylistItemsSource? PlaylistItemsSource);

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



    public sealed class BufferedPlaylistItemsSource : IDisposable, INotifyCollectionChanged
    {
        public const int MaxBufferSize = 2000;
        public const int DefaultBufferSize = 500;

        private readonly IPlaylistItemsSource _playlistItemsSource;
        private PlaylistItem?[] _bufferedItems;

        private readonly int _maxItemsCount;

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public PlaylistItem?[] CopyBufferedItems()
        {
            return _bufferedItems.ToArray();
        }

        public BufferedPlaylistItemsSource(IPlaylistItemsSource playlistItemsSource, int initialBufferSize = DefaultBufferSize)
        {
            _bufferedItems = new PlaylistItem[initialBufferSize];
            _playlistItemsSource = playlistItemsSource;

            if (_playlistItemsSource is IShufflePlaylistItemsSource shufflePlaylist)
            {
                _maxItemsCount = shufflePlaylist.MaxItemsCount;

                // shufflePlaylist.AddedItem 等をハンドルする
                // 可変配列として対応したいのでListを使用したほうが良さそう
            }
            else
            {
                _maxItemsCount = int.MaxValue;
            }
                
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public PlaylistId PlaylistId => _playlistItemsSource.PlaylistId;

        public int OneTimeItemsCount => _playlistItemsSource.OneTimeItemsCount;

        
        public ValueTask<IEnumerable<PlaylistItem>> GetAllAsync(CancellationToken ct = default)
        {
            return GetRangeAsync(0, int.MaxValue, ct);
        }

        public async ValueTask<PlaylistItem?> GetAsync(int index, CancellationToken ct = default)
        {
            return (await GetRangeAsync(index, 1, ct)).FirstOrDefault();
        }

        public async ValueTask<IEnumerable<PlaylistItem>> GetRangeAsync(int start, int count, CancellationToken ct = default)
        {
            Guard.IsBetweenOrEqualTo(start + count, 0, MaxBufferSize, "start + count");

            int block = start / _playlistItemsSource.OneTimeItemsCount;
            int current = block * _playlistItemsSource.OneTimeItemsCount;
            int getTimes = (count + (start - current)) / _playlistItemsSource.OneTimeItemsCount + 1;

            if (_bufferedItems.Length < start + count)
            {
                Array.Resize(ref _bufferedItems, start + count);
            }

            foreach (int getTime in Enumerable.Range(0, getTimes))
            {
                if (_bufferedItems.Skip(current).Take(_playlistItemsSource.OneTimeItemsCount).All(x => x is not null))
                {
                    current += _playlistItemsSource.OneTimeItemsCount;
                    continue;
                }

                var items = await _playlistItemsSource.GetRangeAsync(current, _playlistItemsSource.OneTimeItemsCount, ct);                
                foreach (var item in items)
                {
                    _bufferedItems[current++] = item;
                }
            }

            {
                var items = _bufferedItems.Skip(start).Take(count).ToList();
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, items));
                return items;
            }
        }

    }

    public abstract class PlaylistPlayer : BindableBase
    {
        private const int InvalidIndex = -1;
        private readonly MediaPlayer _mediaPlayer;
        private BufferedPlaylistItemsSource? _bufferedPlaylistItemsSource;

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
            set { SetProperty(ref _currentPlayingIndex, value); }
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

        public PlaylistItem?[]? CopyBufferedItems()
        {
            return _bufferedPlaylistItemsSource?.CopyBufferedItems();
        }

        protected void Reset(IPlaylistItemsSource playlistItemsSource, PlaylistItem current)
        {
            if (playlistItemsSource is IShufflePlaylistItemsSource shufflePlaylist)
            {
                _bufferedPlaylistItemsSource = new BufferedPlaylistItemsSource(playlistItemsSource, shufflePlaylist.MaxItemsCount);
                _maxItemsCount = shufflePlaylist.MaxItemsCount;
                _shuffledIndexies = Enumerable.Range(0, shufflePlaylist.MaxItemsCount).Shuffle().ToArray();
                CurrentPlayingIndex = IndexTranformWithCurrentPlaylistMode(current.ItemIndex);
                IsUnlimitedPlaylistSource = false;
            }
            else
            {
                _bufferedPlaylistItemsSource = new BufferedPlaylistItemsSource(playlistItemsSource);
                _maxItemsCount = null;
                _shuffledIndexies = null;
                CurrentPlayingIndex = current.ItemIndex;
                IsUnlimitedPlaylistSource = true;
            }
        }

        protected void Clear()
        {
            _bufferedPlaylistItemsSource = null;
            _maxItemsCount = null;
            _shuffledIndexies = null;
            CurrentPlayingIndex = InvalidIndex;
        }


        public async ValueTask<PlaylistItem?> GetNextItemAsync(CancellationToken ct = default)
        {
            if (CurrentPlayingIndex == InvalidIndex) { return null; }
            if (_bufferedPlaylistItemsSource == null) { return null; }

            int index = IndexTranformWithCurrentPlaylistMode(CurrentPlayingIndex + 1);
            return await _bufferedPlaylistItemsSource.GetAsync(index, ct);
        }

        public async ValueTask<PlaylistItem?> GetPreviewItemAsync(CancellationToken ct = default)
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
                await PlayAsync_Internal(item);
                CurrentPlayingIndex = index;
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
                await PlayAsync_Internal(item);
                CurrentPlayingIndex = index;
                return true;
            }
            else
            {
                return false;
            }        
        }

        protected abstract Task PlayAsync_Internal(PlaylistItem item, TimeSpan? startPosition = null);

    }



    public sealed class HohoemaPlaylistPlayer : PlaylistPlayer
    {
        private readonly IMessenger _messenger;
        private readonly MediaPlayer _mediaPlayer;
        private readonly VideoStreamingOriginOrchestrator _videoStreamingOriginOrchestrator;
        private readonly IPlaylistItemsSourceResolver _playlistSourceManager;
        private readonly PlayerSettings _playerSettings;
        private readonly MediaPlayerSoundVolumeManager _soundVolumeManager;

        public HohoemaPlaylistPlayer(
            IMessenger messenger,
            MediaPlayer mediaPlayer,
            VideoStreamingOriginOrchestrator videoStreamingOriginOrchestrator,
            IPlaylistItemsSourceResolver playlistSourceManager,
            PlayerSettings playerSettings,
            MediaPlayerSoundVolumeManager soundVolumeManager
            )
        {
            _messenger = messenger;
            _mediaPlayer = mediaPlayer;
            _videoStreamingOriginOrchestrator = videoStreamingOriginOrchestrator;
            _playlistSourceManager = playlistSourceManager;
            _playerSettings = playerSettings;
            _soundVolumeManager = soundVolumeManager;
        }

        public PlaylistId? CurrentPlaylistId => _itemsSource?.PlaylistId;

        private PlaylistItem? _currentPlaylistItem;
        public PlaylistItem? CurrentPlaylistItem
        {
            get { return _currentPlaylistItem; }
            private set { SetProperty(ref _currentPlaylistItem, value); }
        }
        private IPlaylistItemsSource? _itemsSource;

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

        public Task PlayAsync(PlaylistItem item, TimeSpan? startPosition = null)
        {
            return PlayAsync_Internal(item, startPosition);
        }

        protected override async Task PlayAsync_Internal(PlaylistItem item, TimeSpan? startPosition = null)
        {
            Guard.IsNotNull(item, nameof(item));
            Guard.IsFalse(item.ItemId == default(VideoId) && item.PlaylistId == null, "Not contain playable VideoId or PlaylistId");

            if (startPosition == null
                && CurrentPlaylistItem?.ItemId == item.ItemId)
            {
                startPosition = _mediaPlayer.PlaybackSession?.Position;
            }

            StopPlaybackMedia();

            await UpdatePlaylistItemsSourceAsync(item);

            await UpdatePlayingMediaAsync(item, startPosition);
        }

        private async Task UpdatePlayingMediaAsync(PlaylistItem item, TimeSpan? startPosition)
        {
            if (item.ItemId == default(VideoId))
            {
                var firstContent = (await _itemsSource.GetRangeAsync(0, 1)).FirstOrDefault();
                Guard.IsNotNull(firstContent, nameof(firstContent));                
                item = firstContent;
            }

            try
            {
                var result = await _videoStreamingOriginOrchestrator.CreatePlayingOrchestrateResultAsync(item.ItemId);
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

                CurrentPlaylistItem = item;
                RaisePropertyChanged(nameof(AvailableQualities));

                NowPlayingWithCache = videoSession is CachedVideoStreamingSession;
                _soundVolumeManager.LoudnessCorrectionValue = CurrentPlayingSession.VideoDetails.LoudnessCorrectionValue;

                // メディア再生成功時のメッセージを飛ばす
                _messenger.Send(new PlaybackStartedMessage(new(item, videoSession.Quality, _mediaPlayer.PlaybackSession)));
            }
            catch (Exception ex)
            {
                StopPlaybackMedia();
                _messenger.Send(new PlaybackFailedMessage(new(item, ex)));
                throw;
            }
        }

        private void StopPlaybackMedia()
        {
            var prevSource = _mediaPlayer.Source;
            
            var endPosition = _mediaPlayer.PlaybackSession.Position;
            var videoId = CurrentPlaylistItem?.ItemId;

            _videoSessionDisposable?.Dispose();
            _videoSessionDisposable = null;
            CurrentPlayingSession = null;
            _mediaPlayer.Source = null;
            CurrentPlaylistItem = null;

            if (prevSource != null && videoId.HasValue)
            {
                // メディア停止メッセージを飛ばす
                _messenger.Send(new PlaybackStopedMessage(new(videoId.Value, endPosition, PlaybackStopReason.FromUser)));
            }

            _soundVolumeManager.LoudnessCorrectionValue = 1.0;
        }

        private async ValueTask UpdatePlaylistItemsSourceAsync(PlaylistItem? item)
        {
            if (CurrentPlaylistId != null 
                && item != null
                && CurrentPlaylistId == item.PlaylistId)
            {
                return;
            }

            var prevSource = _itemsSource;
            if (item?.PlaylistId != null)
            {
                try
                {
                    _itemsSource = await _playlistSourceManager.ResolveItemsSource(item.PlaylistId);
                    Reset(_itemsSource, item);
                }
                catch
                {
                    _itemsSource = null;
                    _messenger.Send(new ResolvePlaylistFailedMessage(new(item.PlaylistId)));
                }
            }
            else
            {
                _itemsSource = null;
            }

            // プレイリスト更新メッセージを飛ばす
            _messenger.Send(new PlayingPlaylistChangedMessage(new(_itemsSource)));
        }


        public async ValueTask ClearAsync()
        {
            base.Clear();

            StopPlaybackMedia();
            await UpdatePlaylistItemsSourceAsync(null);
        }

        public TimeSpan? GetCurrentPlaybackPosition()
        {
            return _mediaPlayer.PlaybackSession?.Position;
        }
    }
}
