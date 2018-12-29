using Mntone.Nico2;
using NicoPlayerHohoema.Interfaces;
using NicoPlayerHohoema.Models.Helpers;
using Prism.Mvvm;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Windows.Media;
using Windows.Storage;
using Windows.UI.ViewManagement;
using Unity;
using NicoPlayerHohoema.Models.Cache;
using Windows.Media.Playback;
using Windows.Media.Core;
using System.Collections.Specialized;
using Prism.Commands;
using NicoPlayerHohoema.Models;
using NiconicoSession = NicoPlayerHohoema.Models.NiconicoSession;
using NicoPlayerHohoema.Models.LocalMylist;
using System.Reactive.Concurrency;
using AsyncLock = NicoPlayerHohoema.Models.Helpers.AsyncLock;

namespace NicoPlayerHohoema.Services
{
    public delegate void OpenPlaylistItemEventHandler(Interfaces.IMylist playlist, PlaylistItem item);

    public class HohoemaPlaylist : BindableBase, IDisposable
    {
        // Windows10のメディアコントロールとHohoemaのプレイリスト機能を統合してサポート

        // 外部からの次送り、前送り
        // プレイリストリセットなどに対応する
        // 
        // 外部からの操作はイベントに切り出す

        // 画面の遷移自体はPageManagerに任せることにする
        // PageManagerに動画情報を渡すまでをやる

        // TODO: 「あとで見る」プレイリストをローミングフォルダへ書き出す

        public HohoemaPlaylist(
            IScheduler scheduler,
            NiconicoSession niconicoSession,
            VideoCacheManager videoCacheManager,
            PlaylistSettings playlistSettings,
            PlayerViewManager viewMan
            )
        {
            Scheduler = scheduler;
            NiconicoSession = niconicoSession;
            VideoCacheManager = videoCacheManager;
            PlaylistSettings = playlistSettings;
            PlayerViewManager = viewMan;

            Player = new PlaylistPlayer(this, playlistSettings);

            MakeDefaultPlaylist();

            Player.PlayRequested += Player_PlayRequested;


            PlayerViewManager.ObserveProperty(x => x.PlayerViewMode)
                .Subscribe(async newPlayerViewMode => 
                {
                    await Task.Delay(100);

                    if (Player.Current != null)
                    {
                        await PlayerViewManager.PlayWithCurrentPlayerView(Player.Current);
                    }
                });

            // 一般会員は再生とキャッシュDLを１ラインしか許容していないため
            // 再生終了時にキャッシュダウンロードの再開を行う必要がある
            // PlayerViewManager.NowPlaying はSecondaryViewでの再生時にFalseを示してしまうため
            // IsPlayerShowWithSecondaryViewを使ってセカンダリビューでの再生中を検出している
            _resumingObserver = new[]
            {
                // PlayerViewManager.ObserveProperty(x => x.NowPlaying).Select(x => !x),
                PlayerViewManager.ObserveProperty(x => x.IsPlayerShowWithPrimaryView).Select(x => !x),
                PlayerViewManager.ObserveProperty(x => x.IsPlayerShowWithSecondaryView).Select(x => !x),
                NiconicoSession.ObserveProperty(x => x.IsPremiumAccount).Select(x => !x)
            }
            .CombineLatestValuesAreAllTrue()
            .Throttle(TimeSpan.FromSeconds(1))
            .Subscribe(nowResumingCacheDL =>
            {
                Scheduler.Schedule(() => 
                {
                    if (nowResumingCacheDL)
                    {
                        _ = VideoCacheManager.ResumeCacheDownload();

                        // TODO: キャッシュDL再開した場合の通知
                    }
                });
            });
        }

        public const string WatchAfterPlaylistId = "@view";

        public IScheduler Scheduler { get; }
        public NiconicoSession NiconicoSession { get; }
        public VideoCacheManager VideoCacheManager { get; }
        public PlaylistSettings PlaylistSettings { get; private set; }

        public PlaylistPlayer Player { get; }

        public LocalMylistGroup DefaultPlaylist { get; private set; }


        public Interfaces.IMylist CurrentPlaylist => Player.Playlist;
       

        


        private void Player_PlayRequested(object sender, PlaylistItem e)
        {
            Play(e);
        }

        private void Smtc_AutoRepeatModeChangeRequested(SystemMediaTransportControls sender, AutoRepeatModeChangeRequestedEventArgs args)
        {
            PlaylistSettings.RepeatMode = args.RequestedAutoRepeatMode;
            sender.AutoRepeatMode = PlaylistSettings.RepeatMode;
        }

        public void Dispose()
        {
            _resumingObserver?.Dispose();
        }






        AsyncLock SecondaryViewLock = new AsyncLock();
        public PlayerViewManager PlayerViewManager { get; }
        private async Task ShowVideoWithSecondaryView(PlaylistItem item)
        {
            using (var releaser = await SecondaryViewLock.LockAsync())
            {
                if (PlayerViewManager != null)
                {
                    await PlayerViewManager.PlayWithCurrentPlayerView(item);
                }
            }
        }

        public async void Play(PlaylistItem item)
        {
            // プレイリストアイテムが不正
            var playlist = item.Owner;
            if (item.Type == PlaylistItemType.Video)
            {
                if (playlist != null && !playlist.Contains(item.ContentId))
                {
                    throw new Exception();
                }
            }


            if (!NiconicoSession.IsPremiumAccount && !VideoCacheManager.CanAddDownloadLine)
            {
                // 一般ユーザーまたは未登録ユーザーの場合
                // 視聴セッションを１つに制限するため、キャッシュダウンロードを止める必要がある
                // キャッシュ済みのアイテムを再生中の場合はダウンロード可能なので確認をスキップする
                bool playingVideoIsCached = false;
                var currentItem = item;
                if (currentItem != null && currentItem.Type == PlaylistItemType.Video)
                {
                    var cachedItems = await VideoCacheManager.GetCacheRequest(currentItem.ContentId);
                    if (cachedItems.FirstOrDefault(x => x.ToCacheState() == NicoVideoCacheState.Cached) != null)
                    {
                        playingVideoIsCached = true;
                    }
                }

                if (!playingVideoIsCached)
                {
                    var currentDownloadingItems = await VideoCacheManager.GetDownloadProgressVideosAsync();
                    var downloadingItem = currentDownloadingItems.FirstOrDefault();
                    var downloadingItemVideoInfo = Database.NicoVideoDb.Get(downloadingItem.RawVideoId);

                    var dialogService = App.Current.Container.Resolve<Services.DialogService>();
                    var totalSize = downloadingItem.DownloadOperation.Progress.TotalBytesToReceive;
                    var receivedSize = downloadingItem.DownloadOperation.Progress.BytesReceived;
                    var megaBytes = (totalSize - receivedSize) / 1000_000.0;
                    var downloadProgressDescription = $"ダウンロード中\n{downloadingItemVideoInfo.Title}\n残り {megaBytes:0.0} MB ( {receivedSize / 1000_000.0:0.0} MB / {totalSize / 1000_000.0:0.0} MB)";
                    var isCancelCacheAndPlay = await dialogService.ShowMessageDialog("ニコニコのプレミアム会員以外は 視聴とダウンロードは一つしか同時に行えません。\n視聴を開始する場合、キャッシュは中止されます。またキャッシュを再開する場合はダウンロードは最初からやり直しになります。\n\n" + downloadProgressDescription, "キャッシュを中止して視聴を開始しますか？", "キャッシュを中止して視聴する", "何もしない");
                    if (isCancelCacheAndPlay)
                    {
                        await VideoCacheManager.SuspendCacheDownload();
                    }
                    else
                    {
                        return;
                    }
                }
            }
            else if (!NiconicoSession.IsPremiumAccount)
            {
                // キャッシュ済みの場合はサスペンドを掛けない
                if (item.Type == PlaylistItemType.Video && false == await VideoCacheManager.CheckCachedAsync(item.ContentId))
                {
                    await VideoCacheManager.SuspendCacheDownload();
                }
            }

            Player.PlayStarted(item);

            _ = PlayerViewManager.PlayWithCurrentPlayerView(item);
        }


        public void AddWatchAfterPlaylist(string contentId)
        {
            if (!NiconicoRegex.IsVideoId(contentId) && !int.TryParse(contentId, out var temp))
            {
                return;
            }

            var result = DefaultPlaylist.AddMylistItem(contentId, ContentInsertPosition.Tail);
        }


        // あとで見るプレイリストを通じての再生をサポート
        // プレイリストが空だった場合、その場で再生を開始
        public void PlayVideo(string contentId, string title = "", NicoVideoQuality? quality = null)
        {
            if (!NiconicoRegex.IsVideoId(contentId) && !int.TryParse(contentId, out var temp))
            {
                return;
            }

            var result = DefaultPlaylist.AddMylistItem(contentId, ContentInsertPosition.Head);
            Play(new QualityVideoPlaylistItem()
            {
                ContentId = contentId,
                Owner = DefaultPlaylist,
                Title = title,
                Type = PlaylistItemType.Video,
                Quality = quality
            });
        }

        public void PlayVideo(IVideoContent video)
        {
            if (!(NiconicoRegex.IsVideoId(video.Id) || video.Id.All(x => '0' <= x && x <= '9')))
            {
                return;
            }

            DefaultPlaylist.AddMylistItem(video.Id, ContentInsertPosition.Head);

            Play(new QualityVideoPlaylistItem()
            {
                ContentId = video.Id,
                Owner = video.OnwerPlaylist ?? DefaultPlaylist,
                Title = video.Label,
                Type = PlaylistItemType.Video,
            });
        }

        public void Play(IMylist mylist)
        {
            var videoId = mylist.FirstOrDefault();
            if (!(NiconicoRegex.IsVideoId(videoId) || videoId.All(x => '0' <= x && x <= '9')))
            {
                throw new Exception("Video Id not correct format. (\"sm0123456\" or \"12345678\") ");
            }

            Play(new QualityVideoPlaylistItem()
            {
                ContentId = videoId,
                Owner = mylist,
                Type = PlaylistItemType.Video,
            });
        }


        public void PlayLiveVideo(ILiveContent content)
        {
            Play(new LiveVideoPlaylistItem()
            {
                Type = PlaylistItemType.Live,
                ContentId = content.Id,
                Title = content.Label,
                Owner = DefaultPlaylist,
            });
        }

        public void PlayLiveVideo(string liveId, string title = "")
        {
            Play(new LiveVideoPlaylistItem()
            {
                Type = PlaylistItemType.Live,
                ContentId = liveId,
                Title = title,
                Owner = DefaultPlaylist,
            });
        }



        public void PlayDone(PlaylistItem item, bool canPlayNext = false)
        {

            // 次送りが出来る場合は次へ
            if (canPlayNext && (Player?.CanGoNext ?? false))
            {
                Player.GoNext();
            }
            else if (canPlayNext)
            {
                if (PlayerViewManager.IsPlayerShowWithPrimaryView)
                {
                    switch (PlaylistSettings.PlaylistEndAction)
                    {
                        case PlaylistEndAction.ChangeIntoSplit:
                            PlayerViewManager.IsPlayerSmallWindowModeEnabled = true;
                            break;
                        case PlaylistEndAction.CloseIfPlayWithCurrentWindow:
                            PlayerViewManager.ClosePlayer();
                            break;
                    }
                }
            }

            // あとで見るプレイリストの場合、再生後に
            // アイテムを削除する
            if (CurrentPlaylist == DefaultPlaylist)
            {
                DefaultPlaylist.RemoveMylistItem(item.ContentId);
            }
        }


        private void MakeDefaultPlaylist()
        {
            if (DefaultPlaylist == null)
            {
                DefaultPlaylist = new LocalMylistGroup(HohoemaPlaylist.WatchAfterPlaylistId, "あとで見る");
            }
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
                            if (contentId.StartsWith("lv"))
                            {
                                PlayLiveVideo(contentId);
                            }
                            else
                            {
                                PlayVideo(contentId);
                            }
                        }
                        else if (item is IVideoContent videoContent)
                        {
                            PlayVideo(videoContent);
                        }
                        else if (item is PlaylistItem playlistItem)
                        {
                            Play(playlistItem);
                        }
                        else if (item is ILiveContent liveContent)
                        {
                            PlayLiveVideo(liveContent);
                        }
                    },
                    item => item is string || item is IVideoContent || item is PlaylistItem || item is ILiveContent
                    ));
            }
        }


        private DelegateCommand<object> _AddWatchAfterPlaylistCommand;
        public DelegateCommand<object> AddWatchAfterPlaylistCommand
        {
            get
            {
                return _AddWatchAfterPlaylistCommand
                    ?? (_AddWatchAfterPlaylistCommand = new DelegateCommand<object>(item =>
                    {
                        if (item is string contentId)
                        {
                            AddWatchAfterPlaylist(contentId);
                        }
                        else if (item is IVideoContent videoContent)
                        {
                            AddWatchAfterPlaylist(videoContent.Id);
                        }
                        else if (item is PlaylistItem playlistItem)
                        {
                            AddWatchAfterPlaylist(playlistItem.ContentId);
                        }
                    },
                    item => item is string || item is IVideoContent || item is PlaylistItem
                    ));
            }
        }

        public IDisposable _resumingObserver { get; }
    }

    public enum PlayerViewMode
    {
        PrimaryView,
        SecondaryView
    }



    public class PlaylistPlayer : BindableBase, IDisposable
    {
        // PlaylistPlayerは再生アイテムの遷移をサポートする
        // 次・前の移動はPlayRequestedイベントを通じてやり取りする

        // 実際に再生が開始された際に、PlayStartedメソッドの呼び出しが必要

        // シャッフルと通し再生に関する実装の方針
        // 内部ではシャッフル用のランダム化アイテムリストを保持して
        // 常にシャッフル/通しを切り替えられるように備えている



        public event EventHandler<PlaylistItem> PlayRequested;


        private IMylist _Playlist;
        public IMylist Playlist
        {
            get { return _Playlist; }
            private set
            {
                if (SetProperty(ref _Playlist, value))
                {
                    ResetItems();
                }
            }
        }


        private AsyncLock _PlaylistUpdateLock = new AsyncLock();

        public Services.HohoemaPlaylist HohoemaPlaylist { get; }
        public PlaylistSettings PlaylistSettings { get; private set; }

        private PlaylistItem _Current;
        public PlaylistItem Current
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

        protected IList<PlaylistItem> SourceItems { get; } = new List<PlaylistItem>();

        private Queue<PlaylistItem> PlayedItems { get; } = new Queue<PlaylistItem>();
        public List<PlaylistItem> RandamizedItems { get; private set; } = new List<PlaylistItem>();


        IDisposable _SettingsObserveDisposer;
        IDisposable _ItemsObservaeDisposer;
        
        private MediaPlaybackAutoRepeatMode _RepeatMode;
        public MediaPlaybackAutoRepeatMode RepeatMode
        {
            get { return _RepeatMode; }
            set
            {
                if (_RepeatMode != value)
                {
                    _RepeatMode = value;
                    PlaylistSettings.RepeatMode = _RepeatMode;
                }
            }
        }




        public PlaylistPlayer(Services.HohoemaPlaylist hohoemaPlaylist, PlaylistSettings playlistSettings)
        {
            HohoemaPlaylist = hohoemaPlaylist;
            PlaylistSettings = playlistSettings;

            _SettingsObserveDisposer = Observable.Merge(
                PlaylistSettings.ObserveProperty(x => x.IsShuffleEnable).ToUnit(),
                PlaylistSettings.ObserveProperty(x => x.RepeatMode).ToUnit(),
                PlaylistSettings.ObserveProperty(x => x.IsReverseModeEnable).ToUnit()
                )
                .Subscribe(async _ =>
                {
                    using (var releaser = await _PlaylistUpdateLock.LockAsync())
                    {
                        _RepeatMode = PlaylistSettings.RepeatMode;

                        ResetRandmizedItems(SourceItems);

                        CurrentIndex = PlaylistSettings.IsShuffleEnable ? 0 : (SourceItems?.IndexOf(Current) ?? 0);

                        RaisePropertyChanged(nameof(CanGoBack));
                        RaisePropertyChanged(nameof(CanGoNext));
                    }
                });
        }

        public void Dispose()
        {
            _SettingsObserveDisposer?.Dispose();
            _SettingsObserveDisposer = null;
            _ItemsObservaeDisposer?.Dispose();
            _ItemsObservaeDisposer = null;
        }

        private async void ResetItems()
        {
            var newOwner = Playlist;
            using (var releaser = await _PlaylistUpdateLock.LockAsync())
            {
                PlayedItems.Clear();
                _ItemsObservaeDisposer?.Dispose();
                _ItemsObservaeDisposer = null;

                ResetRandmizedItems(SourceItems);
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
                                    Owner = newOwner,
                                    Type = PlaylistItemType.Video,
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

                RaisePropertyChanged(nameof(CanGoBack));
                RaisePropertyChanged(nameof(CanGoNext));
            }
        }

        private void ResetRandmizedItems(IEnumerable<PlaylistItem> items)
        {
            RandamizedItems.Clear();

            if (items != null)
            {
                var firstItem = items.Take(1).SingleOrDefault();
                if (firstItem != null)
                {
                    RandamizedItems.Add(firstItem);
                    RandamizedItems.AddRange(items.Skip(1).Shuffle());
                }
            }
        }

        public bool CanGoBack
        {
            get
            {
                return !PlaylistSettings.IsReverseModeEnable ? __CanGoBack : __CanGoNext;
            }
        }

        private bool __CanGoBack
        {
            get
            {
                if (SourceItems == null) { return false; }

                switch (this.RepeatMode)
                {
                    case MediaPlaybackAutoRepeatMode.None:
                    case MediaPlaybackAutoRepeatMode.Track:
                        if (PlaylistSettings.IsShuffleEnable)
                        {
                            return SourceItems.Count > 1 && PlayedItems.Count > 0;
                        }
                        else
                        {
                            return SourceItems.Count > 1 && CurrentIndex > 0;
                        }
                    case MediaPlaybackAutoRepeatMode.List:
                        if (PlaylistSettings.IsShuffleEnable)
                        {
                            return SourceItems.Count > 1 && PlayedItems.Count > 0;
                        }
                        else
                        {
                            return SourceItems.Count > 1;
                        }
                    default:
                        throw new NotImplementedException();
                }
            }
        }

        
        public void GoBack()
        {
            if (!PlaylistSettings.IsReverseModeEnable)
            {
                __GoBack();
            }
            else
            {
                __GoNext();
            }
        }

        private void __GoBack()
        {
            // Note: CanGoBack で呼び分けが行われている前提の元で例外送出を行っている

            var prevItem = default(PlaylistItem);
            int prevIndex = CurrentIndex - 1;
            if (PlaylistSettings.IsShuffleEnable)
            {
                prevItem = PlayedItems.Dequeue();
            }
            else
            {
                if (prevIndex < 0)
                {
                    if (RepeatMode != MediaPlaybackAutoRepeatMode.List)
                    {
                        throw new Exception();
                    }
                    else
                    {
                        prevIndex = SourceItems.Count - 1;
                    }
                }

                prevItem = SourceItems.ElementAt(prevIndex);
            }
            
            if (prevItem != null)
            {
                Current = prevItem;
                CurrentIndex = prevIndex;

                PlayRequested?.Invoke(this, prevItem);
            }
            else
            {
                throw new Exception();
            }
        }


        public bool CanGoNext
        {
            get { return !PlaylistSettings.IsReverseModeEnable ? __CanGoNext : __CanGoBack; }
        }

        private bool __CanGoNext
        {
            get
            {
                if (SourceItems == null) { return false; }

                switch (this.RepeatMode)
                {
                    case MediaPlaybackAutoRepeatMode.None:
                    case MediaPlaybackAutoRepeatMode.Track:
                        return SourceItems.Count > 1 && SourceItems.Count > (CurrentIndex + 1);
                    case MediaPlaybackAutoRepeatMode.List:
                        return SourceItems.Count > 1;
                    default:
                        throw new NotSupportedException("not support repeat mode : " + RepeatMode.ToString());
                }
            }
        }

        public void GoNext()
        {
            if (!PlaylistSettings.IsReverseModeEnable)
            {
                __GoNext();
            }
            else
            {
                __GoBack();
            }
        }

        private async void __GoNext()
        {
            // Note: CanGoNext で呼び分けが行われている前提の元で例外送出を行っている
            if (SourceItems == null) { throw new Exception(); }

            using (var releaser = await _PlaylistUpdateLock.LockAsync())
            {
                var prevPlayed = Current;
                var nextIndex = CurrentIndex + 1;

                if (nextIndex >= SourceItems.Count)
                {
                    ResetRandmizedItems(SourceItems);
                    nextIndex = 0;
                }

                var nextItem = (PlaylistSettings.IsShuffleEnable ? RandamizedItems : SourceItems)
                    .ElementAt(nextIndex);


                if (nextItem != null)
                {
                    if (prevPlayed != null)
                    {
                        PlayedItems.Enqueue(prevPlayed);
                    }

                    Current = nextItem;
                    CurrentIndex = nextIndex;

                    PlayRequested?.Invoke(this, nextItem);
                }
                else
                {
                    throw new Exception();
                }
            }
        }

        internal async void PlayStarted(PlaylistItem item)
        {
            if (item == null) { throw new Exception(); }

            using (var releaser = await _PlaylistUpdateLock.LockAsync())
            {
                // 新たにプレイリストが指定された場合に
                // 連続再生をセットアップする
                if (item.Owner != null)
                {
                    if (item.Owner != Playlist)
                    {
                        Playlist = item.Owner;
                    }
                }

                SourceItems.Clear();
                foreach (var newItem in Playlist.Select(x => new PlaylistItem()
                {
                    ContentId = x,
                    Owner = Playlist,
                    Type = PlaylistItemType.Video,
                }))
                {
                    SourceItems.Add(newItem);
                }

                RaisePropertyChanged(nameof(CanGoBack));
                RaisePropertyChanged(nameof(CanGoNext));
                //                Current = SourceItems.First(x => item.ContentId == x.ContentId);

                // GoNext/GoBack内でCurrentが既に変更済みの場合はスキップ
                // Playlist外から直接PlaylistItemが変更された場合にのみ
                // 現在再生位置の更新を行う
                if (Current != item)
                {
                    Current = item;
                    if (SourceItems != null)
                    {
                        CurrentIndex = PlaylistSettings.IsShuffleEnable ? 0 : SourceItems.IndexOf(Current);
                    }
                    else
                    {
                        CurrentIndex = 0;
                    }

                    // ランダム再生かつ先頭の再生を行う場合は
                    // ランダムアイテムの先頭が現在再生中アイテムになるように
                    // ランダムアイテムリストを修正する
                    // この修正によって、シャッフル再生が先頭しか再生されない問題を回避できる
                    if (CurrentIndex == 0 && PlaylistSettings.IsShuffleEnable)
                    {
                        if (RandamizedItems.FirstOrDefault() != Current)
                        {
                            RandamizedItems.Remove(Current);
                            RandamizedItems.Insert(0, Current);
                        }
                    }
                }
            }
        }
    }

    
    

    [DataContract]
    public class PlaylistItem : IEquatable<PlaylistItem>, IEqualityComparer<PlaylistItem>
    {
        internal PlaylistItem() { }

        [DataMember]
        public PlaylistItemType Type { get; set; }

        [DataMember]
        public string ContentId { get; set; }

        [DataMember]
        public string Title { get; set; }

        public IMylist Owner { get; set; }

        public bool Equals(PlaylistItem other)
        {
            return this.Type == other.Type
                && this.ContentId == other.ContentId;
        }

        bool IEqualityComparer<PlaylistItem>.Equals(PlaylistItem x, PlaylistItem y)
        {
            return x.Type == y.Type
                && x.ContentId == y.ContentId;
        }

        int IEqualityComparer<PlaylistItem>.GetHashCode(PlaylistItem obj)
        {
            return obj.Type.GetHashCode() ^ (obj.ContentId?.GetHashCode() ?? 0);
        }
    }
    
    public class QualityVideoPlaylistItem : PlaylistItem
    {
        internal QualityVideoPlaylistItem() { }

        public NicoVideoQuality? Quality { get; set; }
    }

    public class LiveVideoPlaylistItem : PlaylistItem
    {
        internal LiveVideoPlaylistItem() { }
    }

    public enum PlaylistItemType
    {
        Video,
        Live,
    }

    public enum PlaylistOrigin
    {
        LoginUser,
        OtherUser,
        Local,
    }

    public enum PlaybackMode
    {
        Through,
        RepeatOne,
        RepeatAll,
    }
    
    
}
