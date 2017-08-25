using Mntone.Nico2;
using NicoPlayerHohoema.Util;
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
using Windows.Media;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;

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


        public MediaPlayer MediaPlayer { get; private set; }
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
                    _CurrentPlaylistId = _CurrentPlaylist.Id;
                    OnPropertyChanged(nameof(Player));

                    ResetMediaPlayerCommand();

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
        


        private bool _IsDisplayPlayer = false;
        public bool IsDisplayPlayer
        {
            get { return _IsDisplayPlayer; }
            set { SetProperty(ref _IsDisplayPlayer, value); }
        }

        private bool _IsDisplayPlayerControlUI = false;
        public bool IsDisplayPlayerControlUI
        {
            get { return _IsDisplayPlayerControlUI; }
            set { SetProperty(ref _IsDisplayPlayerControlUI, value); }
        }


        private bool _IsPlayerFloatingModeEnable = false;
        public bool IsPlayerFloatingModeEnable
        {
            get { return _IsPlayerFloatingModeEnable; }
            set { SetProperty(ref _IsPlayerFloatingModeEnable, value); }
        }

        public HohoemaPlaylist(MediaPlayer mediaPlayer, PlaylistSettings playlistSettings, StorageFolder playlistSaveFolder)
        {
            MediaPlayer = mediaPlayer;
            PlaylistSettings = playlistSettings;
            PlaylistsSaveFolder = playlistSaveFolder;
            Player = new PlaylistPlayer(this, playlistSettings);

            Playlists = new ReadOnlyObservableCollection<LocalMylist>(_Playlists);

            var smtc = MediaPlayer.SystemMediaTransportControls;
            smtc.AutoRepeatModeChangeRequested += Smtc_AutoRepeatModeChangeRequested;
            MediaPlayer.CommandManager.NextReceived += CommandManager_NextReceived;
            MediaPlayer.CommandManager.PreviousReceived += CommandManager_PreviousReceived;

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

            // ファイルがない場合
            if (files.Count == 0)
            {
                // デフォルトプレイリストを作成
                MakeDefaultPlaylist();
                CurrentPlaylist = DefaultPlaylist;

                return;
            }


            // 古いデータを解放
            foreach (var playlist in _Playlists)
            {
                playlist.Dispose();
            }

            _PlaylistFileAccessorMap.Clear();
            _Playlists.Clear();
            DefaultPlaylist = null;


            // 読み込み
            List<LocalMylist> loadedItem = new List<LocalMylist>();
            foreach (var file in files)
            {
                var playlistFileAccessor = new FileAccessor<LocalMylist>(PlaylistsSaveFolder, file.Name);
                var playlist = await playlistFileAccessor.Load();

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

                if (playlist.Id == WatchAfterPlaylistId)
                {
                    DefaultPlaylist = playlist;
                }
            }

            loadedItem.Sort((x, y) => x.SortIndex - y.SortIndex);

            foreach (var sortedPlaylist in loadedItem)
            {
                _Playlists.Add(sortedPlaylist);
            }
            

            // デフォルトプレイリストが削除されていた場合に対応
            if (DefaultPlaylist == null)
            {
                MakeDefaultPlaylist();
            }

            // Live Item は削除
            foreach (var i in DefaultPlaylist.PlaylistItems.Where(x => !NiconicoRegex.IsVideoId(x.ContentId)).ToArray())
            {
                DefaultPlaylist.Remove(i);
            }
        }

        public async Task Save(LocalMylist playlist)
        {
            var fileAccessor = _PlaylistFileAccessorMap[playlist.Id];
            await fileAccessor.Save(playlist);

            await HohoemaApp.PushToRoamingData(await fileAccessor.TryGetFile());
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
            var newFileName = Util.FilePathHelper.ToSafeFilePath(Path.ChangeExtension(newName, ".json"));
            await fileAccessor.Rename(newFileName, forceReplace:true);
            playlist.Name = newName;
            
            await Save(playlist);
        }


        public void Play(PlaylistItem item)
        {
            // プレイリストアイテムが不正
            var playlist = item.Owner;
            if (playlist == null 
                || item == null 
                || !playlist.PlaylistItems.Contains(item)
                )
            {
                throw new Exception();
            }

            Player.Playlist = playlist;
            CurrentPlaylist = playlist;
            Player.PlayStarted(item);

            OpenPlaylistItem?.Invoke(CurrentPlaylist, item);

            IsDisplayPlayer = true;
            
            ResetMediaPlayerCommand();

            MediaPlayer.AudioCategory = MediaPlayerAudioCategory.Media;
        }

        private void CommandManager_PreviousReceived(MediaPlaybackCommandManager sender, MediaPlaybackCommandManagerPreviousReceivedEventArgs args)
        {
            if (args.Handled != true)
            {
                args.Handled = true;

                PlayDone(Player.Current);

                if (Player?.CanGoBack ?? false)
                {
                    Player.GoBack();
                }
            }
        }

        private void CommandManager_NextReceived(MediaPlaybackCommandManager sender, MediaPlaybackCommandManagerNextReceivedEventArgs args)
        {
            if (args.Handled != true)
            {
                args.Handled = true;

                PlayDone(Player.Current);

                if (Player?.CanGoNext ?? false)
                {
                    Player.GoNext();
                }
            }
        }


        private void ResetMediaPlayerCommand()
        {
            var isEnableNextButton = Player?.CanGoNext ?? false;
            if (isEnableNextButton)
            {
                MediaPlayer.CommandManager.NextBehavior.EnablingRule = MediaCommandEnablingRule.Always;
            }
            else
            {
                MediaPlayer.CommandManager.NextBehavior.EnablingRule = MediaCommandEnablingRule.Never;
            }

            var isEnableBackButton = Player?.CanGoBack ?? false;
            if (isEnableBackButton)
            {
                MediaPlayer.CommandManager.PreviousBehavior.EnablingRule = MediaCommandEnablingRule.Always;
            }
            else
            {
                MediaPlayer.CommandManager.PreviousBehavior.EnablingRule = MediaCommandEnablingRule.Never;
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



        public void PlayLiveVideo(string liveId, string title = "")
        {
            OpenPlaylistItem?.Invoke(CurrentPlaylist, new LiveVideoPlaylistItem()
            {
                Type = PlaylistItemType.Live,
                ContentId = liveId,
                Title = title
            });

            IsDisplayPlayer = true;
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

            playlistFileAccessor.Save(playlist).ConfigureAwait(false);

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
                    IsPlayerFloatingModeEnable = true;
                }
                else if (PlaylistSettings.PlaylistEndAction == PlaylistEndAction.CloseIfPlayWithCurrentWindow)
                {
                    IsDisplayPlayer = false;
                }
            }

            // あとで見るプレイリストの場合、再生後に
            // アイテムを削除する
            if (CurrentPlaylist == DefaultPlaylist)
            {
                DefaultPlaylist.Remove(item);
            }

            ResetMediaPlayerCommand();
        }


        private void MakeDefaultPlaylist()
        {
            DefaultPlaylist = CreatePlaylist(WatchAfterPlaylistId, "あとで見る");
        }

        
    }


    public class PlaylistPlayer : IDisposable
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
        }

        internal void ResetItems()
        {
            _InternalPlayer.Reset(Playlist?.PlaylistItems, Current);
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
                .Where(x => currentItem != null && currentItem.ContentId != x.ContentId)
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
