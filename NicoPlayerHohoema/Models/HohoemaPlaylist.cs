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



        private Dictionary<string, FileAccessor<LocalMylist>> _PlaylistFileAccessorMap = new Dictionary<string, FileAccessor<LocalMylist>>();


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
                    
                    _PlaylistItemsChangedObserver?.Dispose();
                    if (CurrentPlaylist != null)
                    {
                        _PlaylistItemsChangedObserver = CurrentPlaylist.PlaylistItems.PropertyChangedAsObservable()
                            .Subscribe(_ => 
                            {
                                Player.ResetItems();
                            });
                    }
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
                var playlistFileAccessor = new FileAccessor<LocalMylist>(PlaylistsSaveFolder, file.Name);
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

                await HohoemaApp.PushToRoamingData(await fileAccessor.TryGetFile());
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

        public void Play(PlaylistItem item)
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

            Player.Playlist = playlist;
            CurrentPlaylist = playlist;
            Player.PlayStarted(item);

            if (PlayerDisplayType == PlayerDisplayType.SecondaryView)
            {
                ShowVideoWithSecondaryView(item).ConfigureAwait(false);
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
            if (!NiconicoRegex.IsVideoId(contentId))
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

            var playlistFileAccessor = new FileAccessor<LocalMylist>(PlaylistsSaveFolder, playlist.Name + ".json");
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
        private IPlayableList _Playlist;
        public IPlayableList Playlist
        {
            get { return _Playlist; }
            set
            {
                if (_Playlist != value)
                {
                    _Playlist = value;
                    ResetPlayer();
                }
            }
        }

        public HohoemaPlaylist HohoemaPlaylist { get; }
        public PlaylistSettings PlaylistSettings { get; private set; }


        IPlaylistPlayer _InternalPlayer;
        public PlaylistItem Current => _InternalPlayer?.Current;

        public bool CanGoBack => _InternalPlayer?.CanGoBack ?? false;

        public bool CanGoNext => _InternalPlayer?.CanGoNext ?? false;

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
                    ResetPlayer();
                }
            }
        }

        private bool _IsShuffleEnable;
        public bool IsShuffleEnable
        {
            get { return _IsShuffleEnable; }
            set
            {
                if (_IsShuffleEnable != value)
                {
                    _IsShuffleEnable = value;
                    ResetPlayer();
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
                .Subscribe(_ =>
                {
                    ResetPlayer();
                });
                

        }

        public void Dispose()
        {
            _SettingsObserveDisposer?.Dispose();
            _SettingsObserveDisposer = null;
            _ItemsObservaeDisposer?.Dispose();
            _ItemsObservaeDisposer = null;
        }

        internal void ResetItems()
        {
            _ItemsObservaeDisposer?.Dispose();
            _ItemsObservaeDisposer = Playlist?.PlaylistItems.CollectionChangedAsObservable()
                .Subscribe(x =>
                {
                    RaisePropertyChanged(nameof(CanGoBack));
                    RaisePropertyChanged(nameof(CanGoNext));
                });
            _InternalPlayer.Reset(Playlist?.PlaylistItems, Current);
            RaisePropertyChanged(nameof(CanGoBack));
            RaisePropertyChanged(nameof(CanGoNext));
        }

        private void ResetPlayer()
        {
            var prevCurrentItem = _InternalPlayer?.Current;

            IPlaylistPlayer newPlayer = null;
            if (PlaylistSettings.IsShuffleEnable)
            {
                newPlayer = new ShufflePlaylistPlayer()
                {
                    IsRepeat = PlaylistSettings.RepeatMode == MediaPlaybackAutoRepeatMode.List
                };
            }
            else
            {
                newPlayer = new ThroughPlaylistPlayer()
                {
                    IsRepeat = PlaylistSettings.RepeatMode == MediaPlaybackAutoRepeatMode.List
                };
            }

            if (newPlayer == null) { throw new Exception(); }

            _InternalPlayer = newPlayer;

            ResetItems();
        }

        public void GoBack()
        {
            var item = _InternalPlayer.GetBackItem();
            if (item != null)
            {
                HohoemaPlaylist.Play(item);
                PlayStarted(item);
            }
        }

        public void GoNext()
        {
            var item = _InternalPlayer.GetNextItem();
            if (item != null)
            {
                HohoemaPlaylist.Play(item);
                PlayStarted(item);
            }
        }

        internal void PlayStarted(PlaylistItem item)
        {
            (_InternalPlayer as PlaylistPlayerBase)?.PlayStarted(item);
            RaisePropertyChanged(nameof(CanGoBack));
            RaisePropertyChanged(nameof(CanGoNext));
        }
    }


    public interface IPlaylistPlayer
    {
        PlaylistItem Current { get; }

        bool CanGoBack { get; }
        bool CanGoNext { get; }

        PlaylistItem GetBackItem();
        PlaylistItem GetNextItem();

        void Reset(IEnumerable<PlaylistItem> list, PlaylistItem currentItem = null);
    }

    abstract public class PlaylistPlayerBase : IPlaylistPlayer
    {
        private List<PlaylistItem> _SourceItems;
        protected IList<PlaylistItem> SourceItems => _SourceItems;
        public PlaylistItem Current { get; protected set; }

        abstract public bool CanGoBack { get; }
        abstract public bool CanGoNext { get; }


        protected bool IsAvailable => _SourceItems != null && _SourceItems.Count > 0;

        public PlaylistPlayerBase()
        {
        }

        internal void PlayStarted(PlaylistItem item)
        {
            Current = item;
        }

        public PlaylistItem GetBackItem()
        {
            return CanGoBack ? GetPreviousItem_Inner() : null;
        }

        public PlaylistItem GetNextItem()
        {
            return CanGoNext ? GetNextItem_Inner() : null;
        }

        abstract protected PlaylistItem GetNextItem_Inner();

        abstract protected PlaylistItem GetPreviousItem_Inner();


        // シャッフルする場合に
        abstract protected void OnResetItems(IEnumerable<PlaylistItem> sourceItems, PlaylistItem currentItem);
        public void Reset(IEnumerable<PlaylistItem> list, PlaylistItem currentItem = null)
        {
            if (list == null)
            {
                currentItem = null;
            }
            else
            {
                if (currentItem != null)
                {
                    if (list.FirstOrDefault(x => x.ContentId == currentItem.ContentId) == null)
                    {
                        currentItem = GetNextItem();

                        // Note: あとで見るプレイリストでの動作としては
                        // 見た後に削除されたあとで、先頭のアイテムが指定されることになります
                    }
                }
            }


            _SourceItems = list?.ToList() ?? new List<PlaylistItem>();
            Current = currentItem;

            OnResetItems(_SourceItems, currentItem);
        }
    }



    public class ThroughPlaylistPlayer : PlaylistPlayerBase
    {
        public ThroughPlaylistPlayer()
            : base()
        {
            
        }



        public override bool CanGoBack
        {
            get
            {
                if (!IsAvailable) { return false; }

                if (SourceItems.Count <= 1) { return false; }

                if (IsRepeat)
                {
                    // 全体リピート時にアイテムが一つの場合は前への移動を制限
                    return true;
                }

                return SourceItems.FirstOrDefault() != Current;
            }
        }

        public override bool CanGoNext
        {
            get
            {
                if (!IsAvailable) { return false; }

                if (SourceItems.Count <= 1) { return false; }

                if (IsRepeat)
                {
                    return true;
                }

                return SourceItems.LastOrDefault() != Current;
            }
        }

        public bool IsRepeat { get; set; } = false;

        protected override PlaylistItem GetPreviousItem_Inner()
        {
            var currentIndex = SourceItems.IndexOf(Current);
            var prevIndex = currentIndex - 1;

            if (prevIndex < 0)
            {
                if (IsRepeat)
                {
                    prevIndex = SourceItems.Count - 1;
                }
                else
                {
                    throw new Exception();
                }
            }

            return SourceItems[prevIndex];
        }


        protected override PlaylistItem GetNextItem_Inner()
        {
            var currentIndex = SourceItems.IndexOf(Current);
            var nextIndex = currentIndex + 1;

            if (nextIndex >= SourceItems.Count)
            {
                if (IsRepeat)
                {
                    nextIndex = 0;
                }
                else
                {
                    throw new Exception();
                }
            }

            return SourceItems[nextIndex];
        }


        protected override void OnResetItems(IEnumerable<PlaylistItem> sourceItems, PlaylistItem currentItem)
        {
            
        }
    }


    // シャッフル実装の参考
    // http://hito-yama-guri.hatenablog.com/entry/2016/01/14/174201

    public class ShufflePlaylistPlayer : PlaylistPlayerBase
    {
        public DateTime LastSyncTime { get; private set; }
        public Queue<PlaylistItem> RandamizedItems { get; private set; } = new Queue<PlaylistItem>();
        public Stack<PlaylistItem> PlayedItem { get; private set; } = new Stack<PlaylistItem>();

        public ShufflePlaylistPlayer()
            : base()
        {
            
        }

        public bool IsRepeat { get; set; }

        public override bool CanGoBack
        {
            get
            {
                if (!IsAvailable) { return false; }

                return PlayedItem.Count > 1;
            }
        }

        public override bool CanGoNext
        {
            get
            {
                if (!IsAvailable) { return false; }

                return IsRepeat ? true : RandamizedItems.Count > 1;
            }
        }

        protected override PlaylistItem GetPreviousItem_Inner()
        {
            // Playedから１つ取り出して
            // Randomizedに載せる
            var prevItem = PlayedItem.Pop();
            if (Current != null && prevItem != null)
            {
                RandamizedItems.Enqueue(Current);

                return prevItem;
            }
            else
            {
                throw new Exception();
            }
        }

        protected override PlaylistItem GetNextItem_Inner()
        {
            if (RandamizedItems.Count == 0)
            {
                if (IsRepeat)
                {
                    // RandamizedItemsを再構成
                    PlayedItem.Clear();

                    OnResetItems(SourceItems, Current);
                }
                else
                {
                    throw new Exception();
                }
            }

            var nextItem = RandamizedItems.Dequeue();
            PlayedItem.Push(Current);

            return nextItem;
        }

        protected override void OnResetItems(IEnumerable<PlaylistItem> sourceItems, PlaylistItem currentItem)
        {
            var copied = sourceItems.ToList();


            // 再生済みアイテムを同期
            var existPlayedItems = PlayedItem.Where(x => copied.Any(y => y.ContentId == x.ContentId));
            PlayedItem = new Stack<PlaylistItem>(existPlayedItems);

            // ランダム化したアイテムの同期
            RandamizedItems.Clear();
            var shuffledUnplayItems = copied
                .Where(x => !PlayedItem.Any(y => x.ContentId == y.ContentId))
                .Where(x => currentItem == null || currentItem.ContentId != x.ContentId)
                .Shuffle();
            foreach (var shuffled in shuffledUnplayItems)
            {
                RandamizedItems.Enqueue(shuffled);
            }

            // Currentを同期
            // 現在再生中のアイテムが削除されていた場合、Currentをnullにするだけ
            if (Current != null && !copied.Any(x => x.ContentId == Current.ContentId))
            {
                Current = null;
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
