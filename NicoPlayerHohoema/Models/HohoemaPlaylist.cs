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
using Microsoft.Practices.Unity;
using NicoPlayerHohoema.Models.Cache;
using Windows.Media.Playback;
using Windows.Media.Core;
using System.Collections.Specialized;
using Prism.Commands;

namespace NicoPlayerHohoema.Models
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
            NiconicoSession niconicoSession,
            VideoCacheManager videoCacheManager,
            PlaylistSettings playlistSettings,
            HohoemaViewManager viewMan
            )
        {
            NiconicoSession = niconicoSession;
            VideoCacheManager = videoCacheManager;
            PlaylistSettings = playlistSettings;
            _SecondaryView = viewMan;

            Player = new PlaylistPlayer(this, playlistSettings);

            MakeDefaultPlaylist();
            CurrentPlaylist = DefaultPlaylist;

            if (ApplicationData.Current.LocalSettings.Values.TryGetValue(nameof(PlayerDisplayType), out var showInMainView))
            {
                try
                {
                    _PlayerDisplayType = (bool)showInMainView ? PlayerDisplayType.PrimaryView : PlayerDisplayType.SecondaryView;
                }
                catch { }
            }

            Player.PlayRequested += Player_PlayRequested;
        }

        public const string WatchAfterPlaylistId = "@view";

        public event OpenPlaylistItemEventHandler OpenPlaylistItem;

        public NiconicoSession NiconicoSession { get; }
        public VideoCacheManager VideoCacheManager { get; }
        public PlaylistSettings PlaylistSettings { get; private set; }

        public PlaylistPlayer Player { get; }

        public LocalMylist.LocalMylistGroup DefaultPlaylist { get; private set; }

        /// <summary>
        /// Use for serialize only.
        /// </summary>
        private string _CurrentPlaylistId { get; set; }

        private Interfaces.IMylist _CurrentPlaylist;
        public Interfaces.IMylist CurrentPlaylist
        {
            get { return _CurrentPlaylist; }
            set
            {
                if (SetProperty(ref _CurrentPlaylist, value))
                {
                    _CurrentPlaylistId = _CurrentPlaylist?.Id;
                    RaisePropertyChanged(nameof(Player));

                    Player.ResetItems(_CurrentPlaylist);
                }
            }
        }


        IDisposable _PlaylistItemsChangedObserver;

        private PlayerDisplayType _PlayerDisplayType = PlayerDisplayType.PrimaryView;
        public PlayerDisplayType PlayerDisplayType
        {
            get { return _PlayerDisplayType; }
            set
            {
                if (value == PlayerDisplayType.SecondaryView && !Services.Helpers.DeviceTypeHelper.IsDesktop)
                {
                    throw new NotSupportedException("Secondary view only Desktop. not support on current device.");
                }

                var prevDisplayType = _PlayerDisplayType;
                if (SetProperty(ref _PlayerDisplayType, value))
                {
                    RaisePropertyChanged(nameof(IsPlayerFloatingModeEnable));

                    Debug.WriteLine("プレイヤー表示状態：" + _PlayerDisplayType.ToString());

                    // TODO: セカンダリビューからの復帰時、Currentがnullになっている
                    var prevItem = Player.Current;
                    if (prevDisplayType == PlayerDisplayType.SecondaryView)
                    {
                        _SecondaryView.Close()
                            .ContinueWith(prevTask =>
                            {
                                if (prevItem != null)
                                {
                                    Play(prevItem);
                                    IsDisplayMainViewPlayer = true;
                                }
                            });
                    }
                    else
                    {
                        // メインビューとセカンダリビューの切り替えが発生した場合にプレイヤーのリセットを行う
                        bool prevMainView = prevDisplayType != PlayerDisplayType.SecondaryView;
                        bool nowMainView = _PlayerDisplayType != PlayerDisplayType.SecondaryView;
                        bool isNeedPlayerReset = prevMainView ^ nowMainView;
                        if (isNeedPlayerReset && prevItem != null)
                        {
                            IsDisplayMainViewPlayer = _PlayerDisplayType != PlayerDisplayType.SecondaryView;

                            Play(prevItem);
                        }
                    }

                    ApplicationView currentView = Windows.UI.ViewManagement.ApplicationView.GetForCurrentView();
                    if (_PlayerDisplayType == PlayerDisplayType.PrimaryView)
                    {
                        if (Services.Helpers.DeviceTypeHelper.IsMobile)
                        {
                            currentView.TryEnterFullScreenMode();
                        }
                        else if (Services.Helpers.DeviceTypeHelper.IsDesktop)
                        {
                            // 
                            if (currentView.AdjacentToLeftDisplayEdge && currentView.AdjacentToRightDisplayEdge)
                            {
                                currentView.TryEnterFullScreenMode();
                            }
                        }
                    }
                    else if (_PlayerDisplayType == PlayerDisplayType.PrimaryWithSmall)
                    {
                        if (ApplicationView.PreferredLaunchWindowingMode != ApplicationViewWindowingMode.FullScreen)
                        {
                            currentView.ExitFullScreenMode();
                        }
                    }

                    ApplicationData.Current.LocalSettings.Values[nameof(PlayerDisplayType)] = _PlayerDisplayType != PlayerDisplayType.SecondaryView;
                }
            }
        }

        private bool _IsDisplayMainViewPlayer = false;
        public bool IsDisplayMainViewPlayer
        {
            get { return _IsDisplayMainViewPlayer; }
            set { SetProperty(ref _IsDisplayMainViewPlayer, value); }
        }

        private bool _IsDisplayPlayerControlUI = false;
        public bool IsDisplayPlayerControlUI
        {
            get { return _IsDisplayPlayerControlUI; }
            set { SetProperty(ref _IsDisplayPlayerControlUI, value); }
        }



        public bool IsPlayerFloatingModeEnable => PlayerDisplayType == PlayerDisplayType.PrimaryWithSmall;




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
            _PlaylistItemsChangedObserver?.Dispose();
        }






        AsyncLock SecondaryViewLock = new AsyncLock();
        HohoemaViewManager _SecondaryView;
        private async Task ShowVideoWithSecondaryView(PlaylistItem item)
        {
            using (var releaser = await SecondaryViewLock.LockAsync())
            {
                if (_SecondaryView != null)
                {
                    await _SecondaryView.OpenContent(item);
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

            CurrentPlaylist = playlist;
            Player.PlayStarted(item);

            if (PlayerDisplayType == PlayerDisplayType.SecondaryView)
            {
                _ = ShowVideoWithSecondaryView(item).ConfigureAwait(false);
            }
            else
            {
                IsDisplayMainViewPlayer = true;

                OpenPlaylistItem?.Invoke(CurrentPlaylist, item);
            }

        }

        private void MediaBinder_Binding(MediaBinder sender, MediaBindingEventArgs args)
        {
            throw new NotImplementedException();
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
                Owner = DefaultPlaylist,
                Title = video.Label,
                Type = PlaylistItemType.Video,
            });
        }


        public void PlayVideoWithPlaylist(IVideoContent video, IMylist mylist)
        {
            if (!(NiconicoRegex.IsVideoId(video.Id) || video.Id.All(x => '0' <= x && x <= '9')))
            {
                throw new Exception("Video Id not correct format. (\"sm0123456\" or \"12345678\") ");
            }

            if (!mylist.Contains(video.Id))
            {
                throw new Exception("mylist not contains videoId. can not play.");
            }

            Play(new QualityVideoPlaylistItem()
            {
                ContentId = video.Id,
                Owner = mylist,
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
                if (PlaylistSettings.PlaylistEndAction == PlaylistEndAction.ChangeIntoSplit)
                {
                    if (PlayerDisplayType == PlayerDisplayType.PrimaryView)
                    {
                        PlayerDisplayType = PlayerDisplayType.PrimaryWithSmall;
                    }
                }
                else if (PlaylistSettings.PlaylistEndAction == PlaylistEndAction.CloseIfPlayWithCurrentWindow)
                {
                    IsDisplayMainViewPlayer = false;
                }
            }

            // あとで見るプレイリストの場合、再生後に
            // アイテムを削除する
            if (CurrentPlaylist == DefaultPlaylist)
            {
                DefaultPlaylist.Remove(item.ContentId);
            }
        }


        private void MakeDefaultPlaylist()
        {
            if (DefaultPlaylist == null)
            {
                DefaultPlaylist = new LocalMylist.LocalMylistGroup(HohoemaPlaylist.WatchAfterPlaylistId, "あとで見る");
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
                            PlayVideo(videoContent.Id);
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


    }

    public enum PlayerDisplayType
    {
        PrimaryView,
        PrimaryWithSmall,
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
                    ResetItems(_Playlist);
                }
            }
        }


        private AsyncLock _PlaylistUpdateLock = new AsyncLock();

        public HohoemaPlaylist HohoemaPlaylist { get; }
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




        public PlaylistPlayer(HohoemaPlaylist hohoemaPlaylist, PlaylistSettings playlistSettings)
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

                        ResetRandmizedItems();

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

        internal async void ResetItems(Interfaces.IMylist newOwner)
        {

            using (var releaser = await _PlaylistUpdateLock.LockAsync())
            {
                PlayedItems.Clear();
                ResetRandmizedItems();
                _ItemsObservaeDisposer?.Dispose();
                _ItemsObservaeDisposer = null;

                Playlist = newOwner;

                if (Playlist is INotifyCollectionChanged playlistNotifyCollectionChanged)
                {
                    _ItemsObservaeDisposer = playlistNotifyCollectionChanged.CollectionChangedAsObservable()
                        .Subscribe(async x =>
                        {
                            using (var releaser2 = await _PlaylistUpdateLock.LockAsync())
                            {
                                // 再生中アイテムが削除された時のプレイリストの動作

                                // 動画プレイヤーには影響を与えないこととする
                                // 連続再生動作の継続性が確保できればOK
                                
                                ResetRandmizedItems();

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

        private void ResetRandmizedItems()
        {
            RandamizedItems.Clear();

            if (SourceItems != null)
            {
                var firstItem = SourceItems.Take(1).SingleOrDefault();
                if (firstItem != null)
                {
                    RandamizedItems.Add(firstItem);
                    RandamizedItems.AddRange(SourceItems.Skip(1).Shuffle());
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
                    ResetRandmizedItems();
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
    public class PlaylistItem
    {
        internal PlaylistItem() { }

        [DataMember]
        public PlaylistItemType Type { get; set; }

        [DataMember]
        public string ContentId { get; set; }

        [DataMember]
        public string Title { get; set; }

        public IMylist Owner { get; set; }
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
    
    public enum ContentInsertPosition
    {
        Head,
        Tail,
    }
}
