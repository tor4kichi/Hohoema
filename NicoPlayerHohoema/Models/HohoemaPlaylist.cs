using NicoPlayerHohoema.Models.Settings;
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
    public delegate void OpenPlaylistItemEventHandler(Playlist playlist, PlaylistItem item);
    
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

        public IPlaylistPlayer Player => CurrentPlaylist?.Player;


        private ObservableCollection<Playlist> _Playlists = new ObservableCollection<Playlist>();
        public ReadOnlyObservableCollection<Playlist> Playlists { get; private set; }



        private Dictionary<string, FileAccessor<Playlist>> _PlaylistFileAccessorMap = new Dictionary<string, FileAccessor<Playlist>>();


        public Playlist DefaultPlaylist { get; private set; }

        /// <summary>
        /// Use for serialize only.
        /// </summary>
        private string _CurrentPlaylistId { get; set; }

        private Playlist _CurrentPlaylist;
        public Playlist CurrentPlaylist
        {
            get { return _CurrentPlaylist; }
            set
            {
                if (SetProperty(ref _CurrentPlaylist, value))
                {
                    _CurrentPlaylistId = _CurrentPlaylist.Id;
                    OnPropertyChanged(nameof(Player));

                    ResetMediaPlayerCommand();
                }
            }
        }

        


        private bool _IsDisplayPlayer = false;
        public bool IsDisplayPlayer
        {
            get { return _IsDisplayPlayer; }
            set { SetProperty(ref _IsDisplayPlayer, value); }
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

            Playlists = new ReadOnlyObservableCollection<Playlist>(_Playlists);

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

        }


        public async Task Load()
        {
            var files = await PlaylistsSaveFolder.GetFilesAsync();

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
            List<Playlist> loadedItem = new List<Playlist>();
            foreach (var file in files)
            {
                var playlistFileAccessor = new FileAccessor<Playlist>(PlaylistsSaveFolder, file.Name);
                var playlist = await playlistFileAccessor.Load();

                if (playlist != null)
                {
                    playlist.HohoemaPlaylist = this;
                    playlist.PlaylistSettings = PlaylistSettings;

                    _PlaylistFileAccessorMap.Add(playlist.Id, playlistFileAccessor);
                    loadedItem.Add(playlist);
                }

                if (playlist.Id == WatchAfterPlaylistId)
                {
                    DefaultPlaylist = playlist;
                }
            }

            loadedItem.Sort((x, y) => x.SortIndex - x.SortIndex);

            foreach (var sortedPlaylist in loadedItem)
            {
                _Playlists.Add(sortedPlaylist);
            }
            

            // デフォルトプレイリストが削除されていた場合に対応
            if (DefaultPlaylist == null)
            {
                MakeDefaultPlaylist();
            }
        }

        

        public async Task Save()
        {
            foreach (var playlist in _Playlists)
            {
                var fileAccessor = _PlaylistFileAccessorMap[playlist.Id];
                await fileAccessor.Save(playlist);
            }
        }



        internal async void RenamePlaylist(Playlist playlist, string newName)
        {
            var fileAccessor = _PlaylistFileAccessorMap[playlist.Id];
            var newFileName = Util.FilePathHelper.ToSafeFilePath(Path.ChangeExtension(newName, ".json"));
            await fileAccessor.Rename(newFileName, forceReplace:true);
            playlist.Name = newName;
            await fileAccessor.Save(playlist);
        }

        internal void PlayStart(Playlist playlist, PlaylistItem item = null)
        {
            // プレイリストアイテムが不正
            if (item != null && !playlist.PlaylistItems.Contains(item))
            {
                throw new Exception();
            }

            CurrentPlaylist = playlist;

            if (item == null)
            {
                item = CurrentPlaylist.PlaylistItems.First();
            }

            OpenVideo(item);
        }


        private void OpenVideo(PlaylistItem item)
        {
            if (item == null)
            {
                throw new NullReferenceException(nameof(PlaylistItem) + " is null.");
            }

            if (item.ContentId == null)
            {
                item.Owner.Remove(item);
                return;
            }

            Player.SetCurrent(item);

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
        public void PlayVideo(string contentId, string title, NicoVideoQuality? quality = null)
        {
            var newItem = DefaultPlaylist.AddVideo(contentId, title, quality);
            //if (DefaultPlaylist.CurrentVideo == null)
            {
                PlayStart(DefaultPlaylist, newItem);
            }
        }



        public void PlayLiveVideo(string liveId, string title)
        {
            var newItem = DefaultPlaylist.AddLiveVideo(liveId, title);
            //if (DefaultPlaylist.CurrentVideo == null)
            {
                PlayStart(DefaultPlaylist, newItem);
            }
        }



        public Playlist CreatePlaylist(string id, string name)
        {
            var sortIndex = _Playlists.Count > 0 ? _Playlists.Max(x => x.SortIndex) + 1 : 0;

            var playlist = new Playlist(id, name)
            {
                HohoemaPlaylist = this,
                PlaylistSettings = PlaylistSettings,
                SortIndex = sortIndex
            };

            var playlistFileAccessor = new FileAccessor<Playlist>(PlaylistsSaveFolder, playlist.Name + ".json");
            _PlaylistFileAccessorMap.Add(playlist.Id, playlistFileAccessor);
            _Playlists.Add(playlist);

            playlistFileAccessor.Save(playlist).ConfigureAwait(false);

            return playlist;
        }

        public void RemovePlaylist(Playlist playlist)
        {
            if (_Playlists.Contains(playlist))
            {
                _Playlists.Remove(playlist);
                var fileAccessor = _PlaylistFileAccessorMap[playlist.Id];
                fileAccessor.Delete().ConfigureAwait(false);
                _PlaylistFileAccessorMap.Remove(playlist.Id);

                playlist.Dispose();
            }
        }

        public void PlayDone()
        {
            // あとで見るプレイリストから再生完了したアイテムを削除する
            if (DefaultPlaylist == CurrentPlaylist)
            {
                if (DefaultPlaylist.CurrentVideo != null)
                {
                    DefaultPlaylist.Remove(DefaultPlaylist.CurrentVideo);
                }
            }

            // 次送りが出来る場合は次へ
            if (Player?.CanGoNext ?? false)
            {
                Player.GoNext();
            }

            ResetMediaPlayerCommand();
        }


        private void MakeDefaultPlaylist()
        {
            DefaultPlaylist = CreatePlaylist(WatchAfterPlaylistId, "あとで見る");
        }

        
    }


    public class PlaylistPlayer : IPlaylistPlayer, IDisposable
    {
        public Playlist Playlist { get; private set; }
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


        public PlaylistPlayer(Playlist playlist, PlaylistSettings playlistSettings)
        {
            Playlist = playlist;
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


        private void ResetPlayer()
        {
            IPlaylistPlayer newPlayer = null;
            if (PlaylistSettings.RepeatMode == MediaPlaybackAutoRepeatMode.Track)
            {
                newPlayer = new RepeatOnePlaylistPlayer(Playlist);
            }
            else if (PlaylistSettings.IsShuffleEnable)
            {
                newPlayer = new ShufflePlaylistPlayer(Playlist)
                {
                    IsRepeat = PlaylistSettings.RepeatMode == MediaPlaybackAutoRepeatMode.List
                };
            }
            else
            {
                newPlayer = new ThroughPlaylistPlayer(Playlist)
                {
                    IsRepeat = PlaylistSettings.RepeatMode == MediaPlaybackAutoRepeatMode.List
                };
            }

            if (newPlayer == null) { throw new Exception(); }

            // 
            var oldPlayer = _InternalPlayer;
            _InternalPlayer = newPlayer;
            if (oldPlayer?.Current != null)
            {
                newPlayer.SetCurrent(oldPlayer.Current);
            }
        }

        public void SetCurrent(PlaylistItem item)
        {
            _InternalPlayer.SetCurrent(item);
        }

        public void GoBack()
        {
            _InternalPlayer.GoNext();
        }

        public void GoNext()
        {
            _InternalPlayer.GoBack();
        }
    }


    public interface IPlaylistPlayer
    {
        Playlist Playlist { get; }
        PlaylistItem Current { get; }

        bool CanGoBack { get; }
        bool CanGoNext { get; }

        void SetCurrent(PlaylistItem item);

        void GoBack();
        void GoNext();
    }

    abstract public class PlaylistPlayerBase : IPlaylistPlayer
    {
        public Playlist Playlist { get; set; }
        public PlaylistItem Current { get; protected set; }

        abstract public bool CanGoBack { get; }
        abstract public bool CanGoNext { get; }

        abstract public void GoBack();
        abstract public void GoNext();

        public PlaylistPlayerBase(Playlist playlist)
        {
            Playlist = playlist;
            Current = Playlist?.CurrentVideo;
        }

        public void SetCurrent(PlaylistItem item)
        {
            if (Playlist.PlaylistItems.Contains(item))
            {
                Playlist.CurrentVideo = item;
                Current = item;
            }
        }
    }



    public class ThroughPlaylistPlayer : PlaylistPlayerBase
    {
        public ThroughPlaylistPlayer(Playlist playlist)
            : base(playlist)
        {
            
        }



        public override bool CanGoBack
        {
            get
            {
                return Playlist.PlaylistItems.FirstOrDefault() != Current;
            }
        }

        public override bool CanGoNext
        {
            get
            {
                if (IsRepeat) { return true; }
                return Playlist.PlaylistItems.LastOrDefault() != Current;
            }
        }

        public bool IsRepeat { get; set; } = false;

        public override void GoBack()
        {
            var currentIndex = Playlist.PlaylistItems.IndexOf(Current);
            var prevIndex = currentIndex - 1;

            if (prevIndex < 0) { throw new Exception(); }

            SetCurrent(Playlist.PlaylistItems[prevIndex]);
        }


        public override void GoNext()
        {
            var currentIndex = Playlist.PlaylistItems.IndexOf(Current);
            var nextIndex = currentIndex + 1;

            if (nextIndex >= Playlist.PlaylistItems.Count)
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

            SetCurrent(Playlist.PlaylistItems[nextIndex]);
        }
    }


    // シャッフル実装の参考
    // http://hito-yama-guri.hatenablog.com/entry/2016/01/14/174201

    public class ShufflePlaylistPlayer : PlaylistPlayerBase
    {
        public DateTime LastSyncTime { get; private set; }
        public Queue<PlaylistItem> RandamizedItems { get; private set; }
        public Stack<PlaylistItem> PlayedItem { get; private set; } = new Stack<PlaylistItem>();

        public ShufflePlaylistPlayer(Playlist playlist)
            : base( playlist)
        {

        }

        public bool IsRepeat { get; set; }

        public override bool CanGoBack
        {
            get
            {
                if (IsRequireSync())
                {
                    SyncPlaylistItems();
                }

                return PlayedItem.Count > 0;
            }
        }

        public override bool CanGoNext
        {
            get
            {
                if (IsRequireSync())
                {
                    SyncPlaylistItems();
                }

                return IsRepeat ? true : RandamizedItems.Count > 0;
            }
        }

        public override void GoBack()
        {
            SyncPlaylistItems();

            // Playedから１つ取り出して
            // Randomizedに載せる
            var prevItem = PlayedItem.Pop();
            if (Current != null && prevItem != null)
            {
                RandamizedItems.Enqueue(Current);

                SetCurrent(prevItem);
            }
        }

        public override void GoNext()
        {
            SyncPlaylistItems();

            if (RandamizedItems.Count == 0 && IsRepeat)
            {
                // RandamizedItemsを再構成
                PlayedItem.Clear();

                SyncPlaylistItems();
            }

            var nextItem = RandamizedItems.Dequeue();
            PlayedItem.Push(Current);

            SetCurrent(nextItem);
        }

        private void SyncPlaylistItems()
        {
            if (Playlist.LastUpdate == LastSyncTime)
            {
                return;
            }

            var copied = Playlist.PlaylistItems.ToList();


            // 再生済みアイテムを同期
            var existPlayedItems = PlayedItem.Where(x => copied.Any(y => y.ContentId == x.ContentId));
            PlayedItem = new Stack<PlaylistItem>(existPlayedItems);

            // ランダム化したアイテムの同期
            RandamizedItems.Clear();
            var shuffledUnplayItems = copied
                .Where(x => !PlayedItem.Any(y => x.ContentId == y.ContentId))
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

        private bool IsRequireSync()
        {
            return LastSyncTime != Playlist.LastUpdate;
        }
    }

    public class RepeatOnePlaylistPlayer : PlaylistPlayerBase
    {
        public RepeatOnePlaylistPlayer(Playlist playlist)
            : base(playlist)
        {
            Current = playlist.CurrentVideo;
        }

        public override bool CanGoBack => false;

        public override bool CanGoNext => false;

        public override void GoBack()
        {
            throw new NotSupportedException();
        }

        public override void GoNext()
        {
            throw new NotSupportedException();
        }
    }

    [DataContract]
    public class Playlist : BindableBase, IDisposable
    {
        public HohoemaPlaylist HohoemaPlaylist { get; internal set; }

        private PlaylistSettings _PlaylistSettings;
        public PlaylistSettings PlaylistSettings
        {
            get { return _PlaylistSettings; }
            internal set
            {
                _PlaylistSettings = value;
                Player = new PlaylistPlayer(this, _PlaylistSettings);
            }

        }

        public PlaylistPlayer Player { get; private set; }

        [DataMember]
        public int SortIndex { get; internal set; }


        [DataMember]
        public string Id { get; private set; }


        private string _Name;

        [DataMember]
        public string Name
        {
            get { return _Name; }
            set
            {
                if (SetProperty(ref _Name, value))
                {
                    // プレイリストの名前が変更されたらファイルの名前も変更
                    if (HohoemaPlaylist != null)
                    {
                        HohoemaPlaylist.RenamePlaylist(this, _Name);
                    }
                }
            }
        }

        [DataMember]
        private ObservableCollection<PlaylistItem> _PlaylistItems { get; set; } = new ObservableCollection<PlaylistItem>();
        public ReadOnlyObservableCollection<PlaylistItem> PlaylistItems { get; private set; }

        IDisposable _ItemsChangedObserveDisposer;

        [DataMember]
        public DateTime LastUpdate { get; private set; }

        [DataMember]
        public DateTime LastPlayed { get; private set; }

        /// <summary>
        /// Use for serialize only.
        /// </summary>
        [DataMember]
        public string _PlayingVideoId { get; set; }

        private PlaylistItem _CurrentVideo;
        public PlaylistItem CurrentVideo
        {
            get { return _CurrentVideo; }
            set { SetProperty(ref _CurrentVideo, value); }
        }


        public Playlist()
        {
            Id = null;
            PlaylistItems = new ReadOnlyObservableCollection<PlaylistItem>(_PlaylistItems);
        }

        public Playlist(string id, string name)
            : this()
        {
            Id = id;
            _Name = name;

            _ItemsChangedObserveDisposer = PlaylistItems.CollectionChangedAsObservable()
                .Subscribe(x => 
                {
                    LastUpdate = DateTime.Now;
                });
        }


        public void Dispose()
        {
            _ItemsChangedObserveDisposer?.Dispose();
            Player?.Dispose();
        }


        [OnDeserialized]
        public void OnSeralized(StreamingContext context)
        {

            if (_PlayingVideoId != null)
            {
                var currentVideoItem = PlaylistItems.FirstOrDefault(x => x.ContentId == _PlayingVideoId);
                if (currentVideoItem != null)
                {
                    _CurrentVideo = currentVideoItem;
                }
                else
                {
                    _PlayingVideoId = null;
                }
            }

            foreach (var item in PlaylistItems)
            {
                item.Owner = this;
            }

            _ItemsChangedObserveDisposer?.Dispose();

            if (_PlaylistItems == null)
            {
                _PlaylistItems = new ObservableCollection<PlaylistItem>();
            }

            PlaylistItems = new ReadOnlyObservableCollection<PlaylistItem>(_PlaylistItems);
            _ItemsChangedObserveDisposer = PlaylistItems.CollectionChangedAsObservable()
                .Subscribe(x =>
                {
                    LastUpdate = DateTime.Now;
                });
        }




        internal void Play(PlaylistItem item)
        {
            HohoemaPlaylist.PlayStart(this, item);
        }

        public PlaylistItem AddVideo(string videoId, string videoName, NicoVideoQuality? quality = null)
        {
            if (videoId == null) { throw new Exception(); }

            // すでに登録済みの場合
            var alreadyAdded = _PlaylistItems.SingleOrDefault(x => x.Type == PlaylistItemType.Video && x.ContentId == videoId);
            if (alreadyAdded != null)
            {
                // 何もしない
                return alreadyAdded;
            }

            var newItem = new QualityVideoPlaylistItem()
            {
                Type = PlaylistItemType.Video,
                ContentId = videoId,
                Title = videoName,
                Quality = quality,
                Owner = this,
            };

            _PlaylistItems.Insert(0, newItem);

            HohoemaPlaylist.Save().ConfigureAwait(false);

            return newItem;
        }

        public PlaylistItem AddLiveVideo(string liveId, string liveName)
        {
            // すでに登録済みの場合
            var alreadyAdded = _PlaylistItems.SingleOrDefault(x => x.Type == PlaylistItemType.Live && x.ContentId == liveId);
            if (alreadyAdded != null)
            {
                // 何もしない
                return alreadyAdded;
            }

            var newItem = new LiveVideoPlaylistItem()
            {
                Type = PlaylistItemType.Live,
                ContentId = liveId,
                Title = liveName,
                Owner = this,
            };

            _PlaylistItems.Insert(0, newItem);

            return newItem;
        }

        public bool Remove(PlaylistItem item)
        {
            if (PlaylistItems.Contains(item))
            {
                if (_PlaylistItems.Remove(item))
                {
                    HohoemaPlaylist.Save().ConfigureAwait(false);

                    return true;
                }
            }

            return false;
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

        public Playlist Owner { get; set; }

        public void Play()
        {
            Owner.Play(this);
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

    public enum PlaybackMode
    {
        Through,
        RepeatOne,
        RepeatAll,
    }
    
}
