using Mntone.Nico2;
using NicoPlayerHohoema.Interfaces;
using NicoPlayerHohoema.Helpers;
using Prism.Mvvm;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Foundation.Metadata;
using Windows.Media;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.UI.ViewManagement;
using Windows.UI.Core;

namespace NicoPlayerHohoema.Models
{
    public delegate void OpenPlaylistItemEventHandler(IPlayableList playlist, PlaylistItem item);
    
    public class HohoemaPlaylist : BindableBase, IDisposable
    {
        // Windows10のメディアコントロールとHohoemaのプレイリスト機能を統合してサポート

        // 外部からの次送り、前送り
        // プレイリストリセットなどに対応する
        // 
        // 外部からの操作はイベントに切り出す

        // 画面の遷移自体はPageManagerに任せることにする
        // PageManagerに動画情報を渡すまでをやる

        // データの保存

        public StorageFolder PlaylistsSaveFolder { get; private set; }


        public const string WatchAfterPlaylistId = "@view";

        public event OpenPlaylistItemEventHandler OpenPlaylistItem;


        public PlaylistSettings PlaylistSettings { get; private set; }

        public PlaylistPlayer Player { get; }


        private ObservableCollection<LocalMylist> _Playlists = new ObservableCollection<LocalMylist>();
        public ReadOnlyObservableCollection<LocalMylist> Playlists { get; private set; }



        private Dictionary<string, FolderBasedFileAccessor<LocalMylist>> _PlaylistFileAccessorMap = new Dictionary<string, FolderBasedFileAccessor<LocalMylist>>();


        public LocalMylist DefaultPlaylist { get; private set; }

        /// <summary>
        /// Use for serialize only.
        /// </summary>
        private string _CurrentPlaylistId { get; set; }

        private IPlayableList _CurrentPlaylist;
        public IPlayableList CurrentPlaylist
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

        private PlayerDisplayType _PlayerDisplayType = Helpers.DeviceTypeHelper.IsDesktop ? PlayerDisplayType.SecondaryView : PlayerDisplayType.PrimaryView;
        public PlayerDisplayType PlayerDisplayType
        {
            get { return _PlayerDisplayType; }
            set
            {
                if (value == PlayerDisplayType.SecondaryView && !DeviceTypeHelper.IsDesktop)
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

                        // Note: 
                        (App.Current as App).PublishInAppNotification(InAppNotificationPayload.CreateReadOnlyNotification(""));
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


        public HohoemaPlaylist(PlaylistSettings playlistSettings, StorageFolder playlistSaveFolder, HohoemaViewManager viewMan)
        {
            PlaylistSettings = playlistSettings;
            PlaylistsSaveFolder = playlistSaveFolder;
            Player = new PlaylistPlayer(this, playlistSettings);
            _SecondaryView = viewMan;

            Playlists = new ReadOnlyObservableCollection<LocalMylist>(_Playlists);

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

        private void Player_PlayRequested(object sender, PlaylistItem e)
        {
            Play(e, isPlayWithUser:false);
        }

        private void Smtc_AutoRepeatModeChangeRequested(SystemMediaTransportControls sender, AutoRepeatModeChangeRequestedEventArgs args)
        {
            PlaylistSettings.RepeatMode = args.RequestedAutoRepeatMode;
            sender.AutoRepeatMode = PlaylistSettings.RepeatMode;
        }

        public void Dispose()
        {
            foreach (var playlist in _Playlists)
            {
                playlist.Dispose();
            }

            _PlaylistItemsChangedObserver?.Dispose();
        }


        public async Task Load()
        {
            var files = await HohoemaApp.GetSyncRoamingData(PlaylistsSaveFolder);

            // 古いデータを解放
            foreach (var playlist in _Playlists)
            {
                playlist.Dispose();
            }

            _PlaylistFileAccessorMap.Clear();
            _Playlists.Clear();

            // 読み込み
            List<LocalMylist> loadedItem = new List<LocalMylist>();
            foreach (var file in files)
            {
                var playlistFileAccessor = new FolderBasedFileAccessor<LocalMylist>(PlaylistsSaveFolder, file.Name);
                var playlist = await playlistFileAccessor.Load();

                
                if (playlist == null || playlist.Id == WatchAfterPlaylistId)
                {
                    await playlistFileAccessor.Delete();
                    continue;
                }

                if (playlist != null)
                {
                    playlist.HohoemaPlaylist = this;

                    // 重複登録されている場合、ファイルの日付が古いほうを削除
                    // （本来はリネームのミスがないようにするべき）
                    if (_PlaylistFileAccessorMap.ContainsKey(playlist.Id))
                    {
                        var prevFileAccessor = _PlaylistFileAccessorMap[playlist.Id];

                        var prevFile = await prevFileAccessor.TryGetFile();
                        var prevFileProp = await prevFile.GetBasicPropertiesAsync();

                        var fileProp = await file.GetBasicPropertiesAsync();
                        if (prevFileProp.DateModified < fileProp.DateModified)
                        {
                            await prevFileAccessor.Delete(StorageDeleteOption.PermanentDelete);
                            _PlaylistFileAccessorMap.Remove(playlist.Id);

                            _PlaylistFileAccessorMap.Add(playlist.Id, playlistFileAccessor);
                            loadedItem.Add(playlist);
                        }
                        else
                        {
                            await HohoemaApp.RoamingDataRemoved(file);
                            await file.DeleteAsync();
                        }
                    }
                    else
                    {
                        _PlaylistFileAccessorMap.Add(playlist.Id, playlistFileAccessor);
                        loadedItem.Add(playlist);
                    }

                }
            }

            loadedItem.Sort((x, y) => x.SortIndex - y.SortIndex);

            foreach (var sortedPlaylist in loadedItem)
            {
                _Playlists.Add(sortedPlaylist);
            }
        }

        public async Task Save(LocalMylist playlist)
        {
            if (_PlaylistFileAccessorMap.ContainsKey(playlist.Id))
            {
                var fileAccessor = _PlaylistFileAccessorMap[playlist.Id];
                await fileAccessor.Save(playlist);

                var file = await fileAccessor.TryGetFile();
                if (file != null)
                {
                    await HohoemaApp.PushToRoamingData(file);
                }
            }
        }

        public async Task Save()
        {
            foreach (var playlist in _Playlists)
            {
                await Save(playlist);
            }
        }



        internal async void RenamePlaylist(LocalMylist playlist, string newName)
        {
            var fileAccessor = _PlaylistFileAccessorMap[playlist.Id];

            // 古いファイルを同期から削除
            var oldFile = await fileAccessor.TryGetFile();
            await HohoemaApp.RoamingDataRemoved(oldFile);

            // ファイル名を変更して保存
            var newFileName = Helpers.FilePathHelper.ToSafeFilePath(Path.ChangeExtension(newName, ".json"));
            await fileAccessor.Rename(newFileName, forceReplace:true);
            playlist.Name = newName;
            
            await Save(playlist);
        }

        AsyncLock SecondaryViewLock = new AsyncLock();
        HohoemaViewManager _SecondaryView;
        private async Task ShowVideoWithSecondaryView(PlaylistItem item, bool withActivationWindow)
        {
            using (var releaser = await SecondaryViewLock.LockAsync())
            {
                if (_SecondaryView != null)
                {
                    await _SecondaryView.OpenContent(item, withActivationWindow);
                }
            }
        }

        public void Play(PlaylistItem item, bool isPlayWithUser = true)
        {
            // プレイリストアイテムが不正
            var playlist = item.Owner;
            if (item.Type == PlaylistItemType.Video)
            {
                if (playlist == null
                    || item == null
                    || playlist.PlaylistItems.FirstOrDefault(x => x.ContentId == item.ContentId) == null
                    )
                {
                    throw new Exception();
                }
            }

            CurrentPlaylist = playlist;
            Player.PlayStarted(item);

            if (PlayerDisplayType == PlayerDisplayType.SecondaryView)
            {
                ShowVideoWithSecondaryView(item, isPlayWithUser).ConfigureAwait(false);
            }
            else
            {
                IsDisplayMainViewPlayer = true;

                OpenPlaylistItem?.Invoke(CurrentPlaylist, item);
            }

        }

        

        // あとで見るプレイリストを通じての再生をサポート
        // プレイリストが空だった場合、その場で再生を開始
        public void PlayVideo(string contentId, string title = "", NicoVideoQuality? quality = null)
        {
            if (!NiconicoRegex.IsVideoId(contentId) && !int.TryParse(contentId, out var temp))
            {
                return;
            }

            var newItem = DefaultPlaylist.AddVideo(contentId, title, ContentInsertPosition.Head);
            Play(newItem);
        }

        public void PlayVideo(IVideoContent video)
        {
            if (!NiconicoRegex.IsVideoId(video.Id))
            {
                return;
            }

            if (video.Playlist != null)
            {
                var playlistItem = video.Playlist.PlaylistItems.FirstOrDefault(x => x.ContentId == video.Id);
                if (playlistItem != null)
                {
                    Play(playlistItem);
                }
            }
            else
            {

                var newItem = DefaultPlaylist.AddVideo(video.Id, video.Label, ContentInsertPosition.Head);
                Play(newItem);
            }
        }


        public void PlayLiveVideo(ILiveContent content)
        {
            Play(new LiveVideoPlaylistItem()
            {
                Type = PlaylistItemType.Live,
                ContentId = content.Id,
                Title = content.Label
            });
        }

        public void PlayLiveVideo(string liveId, string title = "")
        {
            Play(new LiveVideoPlaylistItem()
            {
                Type = PlaylistItemType.Live,
                ContentId = liveId,
                Title = title
            });
        }



        public LocalMylist CreatePlaylist(string id, string name)
        {
            var sortIndex = _Playlists.Count > 0 ? _Playlists.Max(x => x.SortIndex) + 1 : 0;

            var playlist = new LocalMylist(id, name)
            {
                HohoemaPlaylist = this,
                SortIndex = sortIndex
            };

            var playlistFileAccessor = new FolderBasedFileAccessor<LocalMylist>(PlaylistsSaveFolder, playlist.Name + ".json");
            _PlaylistFileAccessorMap.Add(playlist.Id, playlistFileAccessor);
            _Playlists.Add(playlist);

            Save(playlist).ConfigureAwait(false);

            return playlist;
        }

        public async Task RemovePlaylist(LocalMylist playlist)
        {
            if (_Playlists.Contains(playlist))
            {
                _Playlists.Remove(playlist);
                var fileAccessor = _PlaylistFileAccessorMap[playlist.Id];

                var file = await fileAccessor.TryGetFile();
                await HohoemaApp.RoamingDataRemoved(file);

                await fileAccessor.Delete().ConfigureAwait(false);
                _PlaylistFileAccessorMap.Remove(playlist.Id);

                playlist.Dispose();
            }
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
                DefaultPlaylist.Remove(item);
            }
        }


        private void MakeDefaultPlaylist()
        {
            if (DefaultPlaylist == null)
            {
                DefaultPlaylist = CreatePlaylist(WatchAfterPlaylistId, "あとで見る");
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


        private IPlayableList _Playlist;
        public IPlayableList Playlist
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

        protected IList<PlaylistItem> SourceItems => Playlist?.PlaylistItems;

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
                PlaylistSettings.ObserveProperty(x => x.RepeatMode).ToUnit()
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

        internal async void ResetItems(IPlayableList newOwner)
        {

            using (var releaser = await _PlaylistUpdateLock.LockAsync())
            {
                PlayedItems.Clear();
                ResetRandmizedItems();
                _ItemsObservaeDisposer?.Dispose();
                _ItemsObservaeDisposer = null;

                Playlist = newOwner;

                if (Playlist != null)
                {
                    _ItemsObservaeDisposer = Playlist.PlaylistItems.CollectionChangedAsObservable()
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



        public async void GoNext()
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

        public IPlayableList Owner { get; set; }
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

    public enum PlaylistOrigin
    {
        LoginUser,
        OtherUser,
        Local,
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
    
    public enum ContentInsertPosition
    {
        Head,
        Tail,
    }
}
