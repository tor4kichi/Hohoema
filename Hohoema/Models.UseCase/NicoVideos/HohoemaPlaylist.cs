using Hohoema.Models.Domain.Niconico;
using Hohoema.Models.Domain.Niconico.Mylist;
using Hohoema.Models.Domain.Niconico.Mylist.LoginUser;
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Domain.Niconico.Video.WatchHistory.LoginUser;
using Hohoema.Models.Domain.PageNavigation;
using Hohoema.Models.Domain.Player;
using Hohoema.Models.Domain.Playlist;
using Hohoema.Models.Helpers;
using Hohoema.Models.UseCase.Player;
using I18NPortable;
using Microsoft.Toolkit.Mvvm.Messaging;
using Microsoft.Toolkit.Mvvm.Messaging.Messages;
using Mntone.Nico2;
using Prism.Commands;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Uno.Threading;

namespace Hohoema.Models.UseCase.NicoVideos
{
    public class PlayVideoRequestedEventArgs
    {
        internal PlayVideoRequestedEventArgs(IVideoContent requestItem)
        {
            RequestVideoItem = requestItem;
        }
        
        public IVideoContent RequestVideoItem { get; set; }
    }

    public class PlayLiveRequestedEventArgs
    {
        internal PlayLiveRequestedEventArgs(LiveVideoPlaylistItem requestItem)
        {
            RequestLiveItem = requestItem;
        }

        public LiveVideoPlaylistItem RequestLiveItem { get; set; }
    }


    public sealed class QueueItemAddedMessageData
    {
        public string AddedItem { get; internal set; }
    }

    public sealed class QueueItemAddedMessage : ValueChangedMessage<QueueItemAddedMessageData>
    {
        public QueueItemAddedMessage(QueueItemAddedMessageData value) : base(value)
        {
        }
    }


    public sealed class QueueItemRemovedMessageData
    {
        public string RemovedItem { get; internal set; }

    }

    public sealed class QueueItemRemovedMessage : ValueChangedMessage<QueueItemRemovedMessageData>
    {
        public QueueItemRemovedMessage(QueueItemRemovedMessageData value) : base(value)
        {
        }
    }


    public sealed class QueueItemIndexUpdateMessageData
    {
        public string ContentId { get; internal set; }
        public int Index { get; internal set; }
    }

    public sealed class QueueItemIndexUpdateMessage : ValueChangedMessage<QueueItemIndexUpdateMessageData>
    {
        public QueueItemIndexUpdateMessage(QueueItemIndexUpdateMessageData value) : base(value)
        {
        }
    }


    public enum ContentInsertPosition
    {
        Head,
        Tail,
    }

    public static class PlaylistExtension
    {
        public static PlaylistOrigin GetOrigin(this IPlaylist playlist)
        {
            switch (playlist)
            {
                case PlaylistObservableCollection watchLater:
                    return PlaylistOrigin.Local;
                case LocalPlaylist local:
                    return PlaylistOrigin.Local;
                case MylistPlaylist mylist:
                    return PlaylistOrigin.Mylist;
                default:
                    throw new NotSupportedException(playlist?.GetType().Name);
            }
        }

        public static bool IsQueuePlaylist(this IPlaylist localPlaylist)
        {
            return localPlaylist?.Id == HohoemaPlaylist.QueuePlaylistId;
        }

        public static bool IsUniquePlaylist(this IPlaylist playlist)
        {
            return IsQueuePlaylist(playlist);
        }
    }

    public class VideoContentEqualalityComperer : IEqualityComparer<IVideoContent>
    {
        public static VideoContentEqualalityComperer Default { get; } = new VideoContentEqualalityComperer();

        private VideoContentEqualalityComperer() { }

        public bool Equals(IVideoContent x, IVideoContent y)
        {
            return x.Id == y.Id;
        }

        public int GetHashCode(IVideoContent obj)
        {
            return obj.Id.GetHashCode();
        }
    }



    public sealed class VideoPlayedEventArgs
    {
        internal VideoPlayedEventArgs(string videoId, int count)
        {
            VideoId = videoId;
            Count = count;
        }

        public int Count { get; }
        public string VideoId { get; set; }
    }


    public class HohoemaPlaylist : FixPrism.BindableBase, IDisposable
    {
        // Windows10のメディアコントロールとHohoemaのプレイリスト機能を統合してサポート

        // 外部からの次送り、前送り
        // プレイリストリセットなどに対応する
        // 
        // 外部からの操作はイベントに切り出す

        // 画面の遷移自体はPageManagerに任せることにする
        // PageManagerに動画情報を渡すまでをやる


        public const string QueuePlaylistId = "@view";


        static bool IsVideoId(string videoId)
        {
            return NiconicoRegex.IsVideoId(videoId) && !int.TryParse(videoId, out var _);
        }

        public event EventHandler<VideoPlayedEventArgs> VideoPlayed;




        public HohoemaPlaylist(
            IScheduler scheduler,
            PlaylistRepository playlistRepository,
            NicoVideoProvider nicoVideoProvider,
            MylistRepository mylistRepository,
            MylistUserSelectedSortRepository mylistUserSelectedSortRepository,
            VideoPlayedHistoryRepository videoPlayedHistoryRepository,
            PlayerSettings playerSettings
            )
        {
            _scheduler = scheduler;
            
            _player = new PlaylistPlayer(this, playerSettings);
            _player.PlayRequested += OnPlayRequested;

            _player.ObserveProperty(x => x.CanGoNext).Subscribe(_ => _scheduler.Schedule(() => RaisePropertyChanged(nameof(CanGoNext)))).AddTo(_disposable);
            _player.ObserveProperty(x => x.CanGoBack).Subscribe(_ => _scheduler.Schedule(() => RaisePropertyChanged(nameof(CanGoBack)))).AddTo(_disposable);
            _player.ObserveProperty(x => x.Current).Subscribe(_ => _scheduler.Schedule(() => RaisePropertyChanged(nameof(CurrentItem)))).AddTo(_disposable);

            _playlistRepository = playlistRepository;
            _nicoVideoProvider = nicoVideoProvider;
            _mylistRepository = mylistRepository;
            _mylistUserSelectedSortRepository = mylistUserSelectedSortRepository;
            _videoPlayedHistoryRepository = videoPlayedHistoryRepository;
            _playerSettings = playerSettings;

            QueuePlaylist = new PlaylistObservableCollection(QueuePlaylistId, QueuePlaylistId.Translate(), _scheduler);
            _ = ResolveItemsAsync(QueuePlaylist)
                .ContinueWith(prevTask =>
                {
                    var items = prevTask.Result;
                    foreach (var item in items)
                    {
                        AddQueuePlaylist(item);
                    }

                    QueuePlaylist.CollectionChangedAsObservable()
                        .Subscribe(args => PlaylistObservableCollectionChanged(QueuePlaylist, args))
                        .AddTo(_disposable);

                    QueuePlaylist.CollectionChangedAsObservable()                        
                        .Subscribe(args => NotifyQueueItemIndex())
                        .AddTo(_disposable);
                });

            _isShuffleEnabled = playerSettings.IsShuffleEnable;
            _isReverseEnabled = playerSettings.IsReverseModeEnable;
            _isPlaylistLoopingEnabled = playerSettings.IsPlaylistLoopingEnabled;


            /*
            if (newOwner is INotifyCollectionChanged playlistNotifyCollectionChanged)
            {
                _ItemsObservaeDisposer = playlistNotifyCollectionChanged.CollectionChangedAsObservable()
                    .Subscribe(async _ =>
                    {
                        using (var releaser2 = await _PlaylistUpdateLock.LockAsync())
                        {
                                // 再生中アイテムが削除された時のプレイリストの動作

                                // 動画プレイヤーには影響を与えないこととする
                                // 連続再生動作の継続性が確保できればOK

                                SourceItems.Clear();
                            foreach (var newItem in newOwner.Select(x => new PlaylistItem()
                            {
                                ContentId = x,
                            }))
                            {
                                SourceItems.Add(newItem);
                            }

                            ResetRandmizedItems(SourceItems);

                            if (PlaylistSettings.IsShuffleEnable)
                            {
                                CurrentIndex = 0;
                            }
                            else
                            {
                                CurrentIndex = Current == null ? 0 : SourceItems.IndexOf(Current);
                            }

                            RaisePropertyChanged(nameof(CanGoBack));
                            RaisePropertyChanged(nameof(CanGoNext));
                        }
                    });
            }
            */
        }

        CompositeDisposable _disposable = new CompositeDisposable();




        private void PlaylistObservableCollectionChanged(IPlaylist playlist, NotifyCollectionChangedEventArgs args)
        {
            switch (args.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    _playlistRepository.AddItems(playlist.Id, args.NewItems.Cast<IVideoContent>().Select(x => x?.Id).Where(x => !(x is null)));
                    break;
                case NotifyCollectionChangedAction.Move:
                    // TODO: 全消し＆全追加
                    System.Diagnostics.Debug.WriteLine($"[{playlist.Id}] not implement items Move action.");
                    break;
                case NotifyCollectionChangedAction.Remove:
                    _playlistRepository.DeleteItems(playlist.Id, args.OldItems.Cast<IVideoContent>().Select(x => x.Id));
                    break;
                case NotifyCollectionChangedAction.Replace:
                    System.Diagnostics.Debug.WriteLine($"[{playlist.Id}] not implement items Replace action.");
                    break;
                case NotifyCollectionChangedAction.Reset:
                    _playlistRepository.ClearItems(playlist.Id);
                    break;
            }
        }


        private readonly IScheduler _scheduler;
        private readonly PlaylistPlayer _player;
        private readonly PlaylistRepository _playlistRepository;
        private readonly NicoVideoProvider _nicoVideoProvider;
        private readonly MylistRepository _mylistRepository;
        private readonly MylistUserSelectedSortRepository _mylistUserSelectedSortRepository;
        private readonly VideoPlayedHistoryRepository _videoPlayedHistoryRepository;
        private readonly PlayerSettings _playerSettings;

        public PlaylistObservableCollection QueuePlaylist { get; }

        public ReadOnlyObservableCollection<IVideoContent> PlaylistItems => _player.Items;

        public IVideoContent CurrentItem
        {
            get => _player.Current;
        }

        private IPlaylist _currentPlaylist;
        public IPlaylist CurrentPlaylist
        {
            get { return _currentPlaylist; }
            private set { SetProperty(ref _currentPlaylist, value); }
        }


        private bool _isShuffleEnabled;
        public bool IsShuffleEnabled
        {
            get { return _isShuffleEnabled; }
            set
            {
                if (SetProperty(ref _isShuffleEnabled, value))
                {
                    _playerSettings.IsShuffleEnable = value;
                }
            }
        }


        private bool _isPlaylistLoopingEnabled;
        public bool IsPlaylistLoopingEnabled
        {
            get { return _isPlaylistLoopingEnabled; }
            set
            {
                if (SetProperty(ref _isPlaylistLoopingEnabled, value))
                {
                    _playerSettings.IsPlaylistLoopingEnabled = value;
                }
            }
        }


        private bool _isReverseEnabled;
        public bool IsReverseEnable
        {
            get { return _isReverseEnabled; }
            set
            {
                if (SetProperty(ref _isReverseEnabled, value))
                {
                    _playerSettings.IsReverseModeEnable = value;
                }
            }
        }


        void OnPlayRequested(object sender, PlaylistPlayerPlayRequestEventArgs args)
        {
            StrongReferenceMessenger.Default.Send(new PlayerPlayVideoRequestMessage(new() { VideoId = args.Content.Id, Position = args.Position }));

            RaisePropertyChanged(nameof(CurrentItem));
        } 



        


        public override void Dispose()
        {
            base.Dispose();

            _player.PlayRequested -= OnPlayRequested;
            _disposable.Dispose();
        }

        
        public void GoNext()
        {
            if (_player.CanGoNext)
            {
                _player.GoNext();
            }
        }


        public void GoBack()
        {
            if (_player.CanGoBack)
            {
                _player.GoBack();
            }
        }

        public bool CanGoNext
        {
            get => _player.CanGoNext;
        }

        public bool CanGoBack
        {
            get => _player.CanGoBack;
        }

        public async void Play(string videoId, IPlaylist playlist = null, TimeSpan position = default)
        {
            if (!IsVideoId(videoId))
            {
                return;
            }

            var videoContent = await ResolveVideoItemAsync(videoId);
            Play(videoContent, playlist, position);
        }

        public async void Play(IPlaylist playlist)
        {            
            if (playlist != CurrentPlaylist)
            {
                // PlaylistPlayerが内部的にReactiveCollectionを使っており
                // そこでAddOnSchedulerのように非同期操作を行っているため
                // _player.Itemsは同期的に扱えない
                // SetSource使用時は_player.Itemsを回避した初期アイテム設定にする
                IEnumerable<IVideoContent> items = null;
                if (playlist == QueuePlaylist)
                {
                    _player.SetSource(items = QueuePlaylist);
                }
                else
                {
                    _player.SetSource(items = await ResolveItemsAsync(playlist));
                }

                if (items?.Any() != true)
                {
                    Debug.WriteLine("Playが呼ばれたがSetSourceに指定するアイテムリストが得られなかったため再生できない。");
                    return;
                }

                CurrentPlaylist = playlist;
                
                var video = items.FirstOrDefault();

                _player.SetCurrent(video, TimeSpan.Zero);
            }
            else
            {
                var video = _player.Items.FirstOrDefault();
                _player.SetCurrent(video, TimeSpan.Zero);
            }
        }

        public void PlayContinueWithPlaylist(IVideoContent video, IPlaylist playlist, TimeSpan position = default)
        {
            if (CurrentPlaylist != playlist)
            {
                _player.SetSource(Enumerable.Empty<IVideoContent>());
                CurrentPlaylist = null;
            }

            _player.SetCurrent(video, position);
        }

        public async void Play(IVideoContent video, IPlaylist playlist = null, TimeSpan position = default)
        {
            if (CurrentPlaylist != playlist)
            {
                if (playlist == null)
                {
                    _player.SetSource(Enumerable.Empty<IVideoContent>());
                }
                else if (playlist == QueuePlaylist)
                {
                    _player.SetSource(QueuePlaylist);
                }
                else
                {
                    _player.SetSource(await ResolveItemsAsync(playlist));
                }
            }

            CurrentPlaylist = playlist;

            _player.SetCurrent(video, position);
        }


        void NotifyQueueItemIndex()
        {
            var message = new QueueItemIndexUpdateMessage(new());
            int index = 0;
            foreach (var item in QueuePlaylist)
            {
                message.Value.ContentId = item.Id;
                message.Value.Index = index++;
                WeakReferenceMessenger.Default.Send<QueueItemIndexUpdateMessage, string>(message, item.Id);
            }
        }


        public async void AddQueuePlaylist(string videoId, ContentInsertPosition position = ContentInsertPosition.Tail)
        {
            if (!IsVideoId(videoId))
            {
                return;
            }

            var videoContent = await ResolveVideoItemAsync(videoId);

            AddQueuePlaylist(videoContent, position);
        }

        
        public void AddQueuePlaylist(IVideoContent item, ContentInsertPosition position = ContentInsertPosition.Tail)
        {
            QueuePlaylist.RemoveOnScheduler(item);

            if (position == ContentInsertPosition.Tail)
            {
                ResolveVideoItemAsync(item.Id)
                    .ContinueWith(prevTask => QueuePlaylist.AddOnScheduler(prevTask.Result));
            }
            else if (position == ContentInsertPosition.Head)
            {
                ResolveVideoItemAsync(item.Id)
                    .ContinueWith(prevTask => QueuePlaylist.InsertOnScheduler(0, prevTask.Result));;
            }

            WeakReferenceMessenger.Default.Send<QueueItemAddedMessage, string>(new QueueItemAddedMessage(new() { AddedItem = item.Id }), item.Id);
        }

        public void RemoveQueue(IVideoContent item)
        {
            QueuePlaylist.RemoveOnScheduler(item);
            
            WeakReferenceMessenger.Default.Send<QueueItemRemovedMessage, string>(new QueueItemRemovedMessage(new() { RemovedItem = item.Id }), item.Id);
        }

        public int RemoveQueueIfWatched()
        {
            var playedItems = QueuePlaylist.Where(x => _videoPlayedHistoryRepository.IsVideoPlayed(x.Id)).ToList();
            int removeCount = 0;
            foreach (var item in playedItems)
            {
                QueuePlaylist.RemoveOnScheduler(item);
                removeCount++;
            }

            return removeCount;
        }


        public (bool, int) IsQueuePlaylistItem(string id)
        {
            var item = QueuePlaylist.FirstOrDefault(x => x.Id == id);
            if (item == null) { return (false, -1); }
            var index = QueuePlaylist.IndexOf(item);
            return (true, index);
        }

        async Task<IEnumerable<IVideoContent>> ResolveItemsAsync(IPlaylist playlist)
        {
            var playlistItems = new List<IVideoContent>();
            switch (playlist)
            {
                case LoginUserMylistPlaylist loginUserMylist:
                    {
                        var sort = _mylistUserSelectedSortRepository.GetMylistSort(playlist.Id);
                        var loginUserMylistResult = await loginUserMylist.GetAll(sort.SortKey ?? loginUserMylist.DefaultSortKey, sort.SortOrder ?? loginUserMylist.DefaultSortOrder);
                        return loginUserMylistResult;
                    }
                case MylistPlaylist mylist:
                    {
                        var sort = _mylistUserSelectedSortRepository.GetMylistSort(playlist.Id);
                        var mylistResult = await mylist.GetMylistAllItems(sort.SortKey ?? mylist.DefaultSortKey, sort.SortOrder ?? mylist.DefaultSortOrder);
                        return mylistResult.Items;
                    }
                case LocalPlaylist localPlaylist:
                    {
                        var items = _playlistRepository.GetItems(playlist.Id);
                        foreach (var item in items)
                        {
                            var videoContent = await ResolveVideoItemAsync(item.ContentId);
                            playlistItems.Add(videoContent);
                        }
                    }
                    
                    break;
                case PlaylistObservableCollection specialLocalPlaylist:
                    {
                        var items = _playlistRepository.GetItems(specialLocalPlaylist.Id);
                        foreach (var item in items)
                        {
                            var videoContent = await ResolveVideoItemAsync(item.ContentId);
                            playlistItems.Add(videoContent);
                        }
                    }

                    break;
                //case PlaylistOrigin.ChannelVideos:
                //case PlaylistOrigin.UserVideos:
                default:
                    throw new NotSupportedException(playlist.GetOrigin().ToString());
            }

            return playlistItems;
        }

        async Task<NicoVideo> ResolveVideoItemAsync(string videoId)
        {
            return await _nicoVideoProvider.GetNicoVideoInfo(videoId);
        }

        public void PlayDone(IVideoContent playedItem, TimeSpan playedPosition)
        {
            // キュープレイリストから視聴済みを削除
            var queueItem = QueuePlaylist.FirstOrDefault(x => x.Id == playedItem.Id);
            if (queueItem != null)
            {
                RemoveQueue(queueItem);
            }

            // アイテムを視聴済みにマーク
            var history = _videoPlayedHistoryRepository.VideoPlayed(playedItem.Id, playedPosition);
            System.Diagnostics.Debug.WriteLine("視聴完了: " + playedItem.Label);

            // 視聴完了のイベントをトリガー
            VideoPlayed?.Invoke(this, new VideoPlayedEventArgs(playedItem.Id, (int)history.PlayCount));

            StrongReferenceMessenger.Default.Send(new Events.VideoPlayedMessage(new () { ContentId = playedItem.Id, PlayedPosition = playedPosition }), playedItem.Id);
        }


        public bool PlayDoneAndTryMoveNext(TimeSpan playedPosition)
        {
            var playedItem = _player.Current;
            var result = (_player?.CanGoNext ?? false);
            // 次送りが出来る場合は次へ
            if (result)
            {
                _player.GoNext();
            }

            Task.Delay(250).ContinueWith(_ => 
            {
                PlayDone(playedItem, playedPosition);
            });

            return result;
        }


        private DelegateCommand<object> _PlayCommand;
        public DelegateCommand<object> PlayCommand
        {
            get
            {
                return _PlayCommand
                    ?? (_PlayCommand = new DelegateCommand<object>(item =>
                    {
                        if (item is string contentId)
                        {
                            Play(contentId);
                        }
                        else if (item is IVideoContent playlistItem)
                        {
                            Play(playlistItem);
                        }
                    },
                    item => item is string || item is IVideoContent
                    ));
            }
        }
    }

    public class PlaylistPlayerPlayRequestEventArgs
    {
        public IVideoContent Content { get; set; }
        public TimeSpan Position { get; set; }
    }

    public sealed class PlaylistPlayer : FixPrism.BindableBase, IDisposable
    {
        // PlaylistPlayerは再生アイテムの遷移をサポートする
        // 次・前の移動はPlayRequestedイベントを通じてやり取りする

        // 実際に再生が開始された際に、PlayStartedメソッドの呼び出しが必要

        // シャッフルと通し再生に関する実装の方針
        // 内部ではシャッフル用のランダム化アイテムリストを保持して
        // 常にシャッフル/通しを切り替えられるように備えている



        public event EventHandler<PlaylistPlayerPlayRequestEventArgs> PlayRequested;

        Random _shuffleRandom = new Random();

        private FastAsyncLock _PlaylistUpdateLock = new FastAsyncLock();

        public HohoemaPlaylist HohoemaPlaylist { get; }
        public PlayerSettings PlayerSettings { get; private set; }

        private IVideoContent _Current;
        public IVideoContent Current
        {
            get { return _Current; }
            private set
            {
                if (SetProperty(ref _Current, value))
                {
                    RaisePropertyChanged(nameof(CanGoBack));
                    RaisePropertyChanged(nameof(CanGoNext));
                }
            }
        }

        private int CurrentIndex { get; set; }

        ReactiveCollection<IVideoContent> _items = new ReactiveCollection<IVideoContent>();
        public ReadOnlyObservableCollection<IVideoContent> Items { get; }

        private List<IVideoContent> _sourceItems = new List<IVideoContent>();
        IDisposable _SettingsObserveDisposer;
        IDisposable _ItemsObservaeDisposer;
        
        public PlaylistPlayer(HohoemaPlaylist hohoemaPlaylist, PlayerSettings playerSettings)
        {
            HohoemaPlaylist = hohoemaPlaylist;
            PlayerSettings = playerSettings;

            Items = new ReadOnlyObservableCollection<IVideoContent>(_items);

            CompositeDisposable disposables = new CompositeDisposable();

            PlayerSettings.ObserveProperty(x => x.IsReverseModeEnable)
                .Subscribe(async x => 
                {
                    using (var releaser = await _PlaylistUpdateLock.LockAsync(default))
                    {
                        ResetItems();
                    }
                })
                .AddTo(disposables);



            PlayerSettings.ObserveProperty(x => x.IsShuffleEnable)
                .Subscribe(async _ =>
                {
                    using (var releaser = await _PlaylistUpdateLock.LockAsync(default))
                    {
                        _shuffleRandom = new Random();
                        ResetItems();
                    }
                })
                .AddTo(disposables);

            _SettingsObserveDisposer = disposables;
        }


        public void Dispose()
        {
            _SettingsObserveDisposer?.Dispose();
            _SettingsObserveDisposer = null;
            _ItemsObservaeDisposer?.Dispose();
            _ItemsObservaeDisposer = null;
        }

        internal async void SetSource(IEnumerable<IVideoContent> items)
        {
            using (var releaser = await _PlaylistUpdateLock.LockAsync(default))
            {
                _sourceItems.Clear();
                _ItemsObservaeDisposer?.Dispose();

                if (items != null)
                {
                    _sourceItems.AddRange(items);

                    if (items is ObservableCollection<IVideoContent> collection)
                    {
                        _ItemsObservaeDisposer = collection.CollectionChangedAsObservable()
                            .Do(args =>
                            {
                                _sourceItems.Clear();
                                _sourceItems.AddRange(collection);

                                ResetItems();

                                Debug.WriteLine("Playlist Updated.");
                            })
                            .Subscribe();
                    }
                }

                ResetItems();
            }
        }


        void ResetItems()
        {
            IEnumerable<IVideoContent> currentSource = _sourceItems;
            if (PlayerSettings.IsShuffleEnable)
            {
                currentSource = currentSource.Shuffle(_shuffleRandom);
            }

            if (PlayerSettings.IsReverseModeEnable)
            {
                currentSource = currentSource.Reverse();
            }
            var items = currentSource.ToList();

            _items.ClearOnScheduler();
            _items.AddRangeOnScheduler(items);

            CurrentIndex = items.Any() ? items.IndexOf(Current) : 0;

            RaisePropertyChanged(nameof(CanGoBack));
            RaisePropertyChanged(nameof(CanGoNext));
        }


        public bool CanGoBack => __CanGoBack;

        private bool __CanGoBack
        {
            get
            {
                if (!_items.Any()) { return false; }

                if (this.PlayerSettings.IsPlaylistLoopingEnabled)
                {
                    return true;
                }
                else
                {
                    return _items.Count >= 2 && CurrentIndex >= 1;
                }
            }
        }

        
        public void GoBack()
        {
            __GoBack();
        }

        private void __GoBack()
        {
            if (!_items.Any()) { return; }

            var prevItem = default(IVideoContent);
            int prevIndex = CurrentIndex - 1;
            if (prevIndex < 0)
            {
                prevIndex = _items.Count - 1;
            }

            prevItem = _items.ElementAt(prevIndex);

            if (prevItem != null)
            {
                Current = prevItem;
                CurrentIndex = prevIndex;

                PlayRequested?.Invoke(this, new PlaylistPlayerPlayRequestEventArgs() { Content = prevItem });
            }
            else
            {
                throw new Exception();
            }
        }


        public bool CanGoNext
        {
            get { return __CanGoNext; }
        }

        private bool __CanGoNext
        {
            get
            {
                if (!_items.Any()) { return false; }

                if (PlayerSettings.IsPlaylistLoopingEnabled)
                {
                    return true;
                }
                else
                {
                    return _items.Count >= 2 && _items.Count > (CurrentIndex + 1);
                }
            }
        }

        public void GoNext()
        {
            __GoNext();
        }

        private async void __GoNext()
        {
            using (var releaser = await _PlaylistUpdateLock.LockAsync(default))
            {
                if (!_items.Any()) { return; }

                var prevPlayed = Current;
                var nextIndex = CurrentIndex + 1;

                if (nextIndex >= _sourceItems.Count)
                {
                    ResetItems();
                    nextIndex = 0;
                }

                var nextItem = _items
                    .ElementAt(nextIndex);

                if (nextItem != null)
                {
                    Current = nextItem;
                    CurrentIndex = nextIndex;

                    PlayRequested?.Invoke(this, new PlaylistPlayerPlayRequestEventArgs() { Content = nextItem });
                }
                else
                {
                    throw new Exception();
                }
                
            }
        }

        internal async void SetCurrent(IVideoContent item, TimeSpan position)
        {
            if (item == null) { throw new Exception(); }

            using (var releaser = await _PlaylistUpdateLock.LockAsync(default))
            {
                // GoNext/GoBack内でCurrentが既に変更済みの場合はスキップ
                // Playlist外から直接PlaylistItemが変更された場合にのみ
                // 現在再生位置の更新を行う
                Current = item;
                if (_items.Any())
                {
                    CurrentIndex = _items.IndexOf(Current);
                }
                else
                {
                    CurrentIndex = 0;
                }

                PlayRequested?.Invoke(this, new PlaylistPlayerPlayRequestEventArgs() { Content = Current, Position = position });
            }
        }
    }

    public sealed class PlaylistObservableCollection : ReactiveCollection<IVideoContent>, IPlaylist
    {
        public PlaylistObservableCollection(string id, string label, IScheduler scheduler)
            :base(scheduler)
        {
            Id = id;
            Label = label;
        }

        public int SortIndex => 0;

        public string Id { get; }

        public string Label { get; }

        public Uri[] ThumbnailImages => null;

        public Uri ThumbnailImage => null;
    }
    
    public class LiveVideoPlaylistItem : INiconicoContent
    {
        public LiveVideoPlaylistItem(string contentId, string title)
        {
            ContentId = contentId;
            Title = title;
        }

        [DataMember]
        public string ContentId { get; }

        [DataMember]
        public string Title { get; }

        [IgnoreDataMember]
        string INiconicoObject.Id => ContentId;

        [IgnoreDataMember]
        string INiconicoObject.Label => Title;
    }

    public enum PlaylistItemType
    {
        Video,
        Live,
    }

    

    public enum PlaybackMode
    {
        Through,
        RepeatOne,
        RepeatAll,
    }
    
    
}
