using Hohoema.Models.Domain.Niconico.Player;
using Hohoema.Models.Domain.Niconico.Video;
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


    public record PlaybackFailedMessageData(PlaylistItem Item, NicoVideoQuality? VideoQuality, Exception Exception);

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
            Guard.IsBetween(start + count, 0, MaxBufferSize, "start + count");

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
                    RaisePropertyChanged(nameof(IsRepeatModeEnabled));
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

        private bool _isRepeatModeRequested;
        public bool IsRepeatModeRequested
        {
            get { return _isRepeatModeRequested; }
            set
            {
                if (SetProperty(ref _isRepeatModeRequested, value))
                {
                    RaisePropertyChanged(nameof(IsRepeatModeEnabled));
                }
            }
        }
        public bool IsRepeatModeEnabled => IsShuffleAndRepeatAvairable && IsRepeatModeRequested;


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
                CurrentPlayingIndex = ToShuffledIndex(current.ItemIndex);
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

        private int ToShuffledIndex(int index)
        {
            Guard.IsNotNull(_shuffledIndexies, nameof(_shuffledIndexies));

            return _shuffledIndexies[index];
        }

        public async ValueTask<PlaylistItem?> GetNextItemAsync(CancellationToken ct = default)
        {
            if (CurrentPlayingIndex == InvalidIndex) { return null; }

            int index = IndexTranformWithCurrentPlaylistMode(CurrentPlayingIndex + 1);
            return await _bufferedPlaylistItemsSource.GetAsync(ToShuffledIndex(index), ct);
        }

        public async ValueTask<PlaylistItem?> GetPreviewItemAsync(CancellationToken ct = default)
        {
            if (CurrentPlayingIndex == InvalidIndex) { return null; }

            int index = IndexTranformWithCurrentPlaylistMode(CurrentPlayingIndex - 1);
            return await _bufferedPlaylistItemsSource.GetAsync(ToShuffledIndex(index), ct);
        }

        int IndexTranformWithCurrentPlaylistMode(int index)
        {
            int transformed = index;
            if (IsRepeatModeEnabled && _maxItemsCount.HasValue)
            {
                if (index >= 0)
                {
                    transformed = index % _maxItemsCount.Value;
                }
                else
                {
                    Guard.IsEqualTo(-1, index, nameof(index));
                    transformed = index + _maxItemsCount.Value;
                }
            }

            return IsShuffleModeEnabled ? ToShuffledIndex(transformed) : transformed;
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
            Guard.IsBetween(index, 0, _maxItemsCount ?? 5000, nameof(index));
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
            Guard.IsBetween(index, 0, _maxItemsCount ?? 5000, nameof(index));
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

        protected abstract Task PlayAsync_Internal(PlaylistItem item, NicoVideoQuality? nicoVideoQuality = null, TimeSpan? startPosition = null);

    }



    public sealed class HohoemaPlaylistPlayer : PlaylistPlayer
    {
        private readonly IMessenger _messenger;
        private readonly MediaPlayer _mediaPlayer;
        private readonly VideoStreamingOriginOrchestrator _videoStreamingOriginOrchestrator;
        private readonly IPlaylistItemsSourceResolver _playlistSourceManager;

        public HohoemaPlaylistPlayer(
            IMessenger messenger,
            MediaPlayer mediaPlayer,
            VideoStreamingOriginOrchestrator videoStreamingOriginOrchestrator,
            IPlaylistItemsSourceResolver playlistSourceManager
            )
        {
            _messenger = messenger;
            _mediaPlayer = mediaPlayer;
            _videoStreamingOriginOrchestrator = videoStreamingOriginOrchestrator;
            _playlistSourceManager = playlistSourceManager;
        }

        public PlaylistId? CurrentPlaylistId => _itemsSource?.PlaylistId;
        public PlaylistItem? CurrentPlaylistItem { get; private set; }
        private IPlaylistItemsSource? _itemsSource;

        public PlayingOrchestrateResult CurrentPlayingSession { get; private set; }

        IDisposable _videoSessionDisposable;

        public Task PlayAsync(PlaylistItem item, NicoVideoQuality? nicoVideoQuality = null, TimeSpan? startPosition = null)
        {
            return PlayAsync_Internal(item, nicoVideoQuality, startPosition);
        }

        protected override async Task PlayAsync_Internal(PlaylistItem item, NicoVideoQuality? nicoVideoQuality = null, TimeSpan? startPosition = null)
        {
            Guard.IsNotNull(item, nameof(item));
            Guard.IsFalse(item.ItemId == default(VideoId) && item.PlaylistId == null, "Not contain playable VideoId or PlaylistId");            

            bool isSameVideo = CurrentPlaylistItem is not null && CurrentPlaylistItem.ItemId == item.ItemId;
            if (!isSameVideo)
            {
                StopPlaybackMedia();
            }

            await UpdatePlaylistItemsSourceAsync(item);

            if (!isSameVideo)
            {
                await UpdatePlayingMediaAsync(item, nicoVideoQuality, startPosition);
            }
        }

        private async Task UpdatePlayingMediaAsync(PlaylistItem item, NicoVideoQuality? nicoVideoQuality, TimeSpan? startPosition)
        {
            if (item.ItemId == default(VideoId))
            {
                var firstContent = (await _itemsSource.GetRangeAsync(0, 1)).FirstOrDefault();
                Guard.IsNotNull(firstContent, nameof(firstContent));                
                item = firstContent;
            }

            try
            {
                CurrentPlaylistItem = item;

                var result = await _videoStreamingOriginOrchestrator.CreatePlayingOrchestrateResultAsync(item.ItemId);
                CurrentPlayingSession = result;
                if (!result.IsSuccess)
                {
                    throw new HohoemaExpception("failed playing start.", result.Exception);
                }

                var videoSession = await result.VideoSessionProvider.CreateVideoSessionAsync(nicoVideoQuality ?? result.VideoSessionProvider.AvailableQualities.LastOrDefault()?.Quality ?? throw new HohoemaExpception());

                _videoSessionDisposable = videoSession;
                await videoSession.StartPlayback(_mediaPlayer, startPosition ?? TimeSpan.Zero);

                Guard.IsNotNull(_mediaPlayer.PlaybackSession, nameof(_mediaPlayer.PlaybackSession));


                // メディア再生成功時のメッセージを飛ばす
                _messenger.Send(new PlaybackStartedMessage(new(item, videoSession.Quality, _mediaPlayer.PlaybackSession)));
            }
            catch (Exception ex)
            {
                StopPlaybackMedia();
                _messenger.Send(new PlaybackFailedMessage(new(item, nicoVideoQuality, ex)));
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
    }
}
