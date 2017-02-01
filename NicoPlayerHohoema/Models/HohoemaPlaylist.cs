using NicoPlayerHohoema.Models.Settings;
using NicoPlayerHohoema.Util;
using Prism.Mvvm;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Windows.Media;
using Windows.Media.Playback;

namespace NicoPlayerHohoema.Models
{

    // TODO: Playlist全体のDisposeの流れ
    
    // TODO: Playlist.PlaylistItemsが更新された時のPlayerの再生成をObservableCollection経由で行う



    public delegate void OpenPlaylistItemEventHandler(Playlist playlist, PlaylistItem item);
    
    public class HohoemaPlaylist : BindableBase
    {
        // Windows10のメディアコントロールとHohoemaのプレイリスト機能を統合してサポート

        // 外部からの次送り、前送り
        // プレイリストリセットなどに対応する
        // 
        // 外部からの操作はイベントに切り出す

        // 画面の遷移自体はPageManagerに任せることにする
        // PageManagerに動画情報を渡すまでをやる

        // データの保存

        

        public const string WatchAfterPlaylistId = "@view";

        public event OpenPlaylistItemEventHandler OpenPlaylistItem;


        public MediaPlayer MediaPlayer { get; private set; }
        public PlaylistSettings PlaylistSettings { get; private set; }

        public IPlaylistPlayer Player { get; private set; }


        public ObservableCollection<Playlist> Playlists { get; private set; } = new ObservableCollection<Playlist>();

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
                    MakePlayer();
                }
            }
        }

        public bool CanGoNext => Player.CanGoNext;

        public bool CanGoBack => Player.CanGoBack;

        public void GoNext()
        {
            Player.GoNext();

            if (Player.Current != null)
            {
                OpenVideo(Player.Current);
            }
        }

        public void GoBack()
        {
            Player.GoBack();

            if (Player.Current != null)
            {
                OpenVideo(Player.Current);
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



        public HohoemaPlaylist(MediaPlayer mediaPlayer, PlaylistSettings playlistSettings)
        {
            MediaPlayer = mediaPlayer;
            PlaylistSettings = playlistSettings;
            DefaultPlaylist = CreatePlaylist("@view", "あとで見る");
            CurrentPlaylist = DefaultPlaylist;

            Observable.Merge(
                PlaylistSettings.ObserveProperty(x => x.IsShuffleEnable).ToUnit(),
                PlaylistSettings.ObserveProperty(x => x.RepeatMode).ToUnit()
                )
                .Subscribe(x => MakePlayer());
                        
            MakePlayer();
        }



        [OnDeserialized]
        public void OnSeralized(StreamingContext context)
        {
            if (_CurrentPlaylistId != null)
            {
                var currentPlaylist = Playlists.FirstOrDefault(x => x.Id == _CurrentPlaylistId);
                if (currentPlaylist != null)
                {
                    _CurrentPlaylist = currentPlaylist;
                }
                else
                {
                    _CurrentPlaylistId = null;
                }
            }

            foreach (var playlist in Playlists)
            {
                playlist.HohoemaPlaylist = this;
            }
        }





        private void MakePlayer()
        {
            // プレイリストが同一なままPlayerが再生成された場合に
            // 再生中アイテムの引き継ぎを行う
            // （そもそも再生中アイテムをPlayerに持たせるべきか？）

            

            IPlaylistPlayer newPlayer = null;
            if (PlaylistSettings.RepeatMode == PlaybackMode.RepeatOne)
            {
                newPlayer = new RepeatOnePlaylistPlayer(this, CurrentPlaylist);
            }
            else if (PlaylistSettings.IsShuffleEnable)
            {
                newPlayer = new ShufflePlaylistPlayer(this, CurrentPlaylist)
                {
                    IsRepeat = PlaylistSettings.RepeatMode == PlaybackMode.RepeatAll
                };
            }
            else
            {
                newPlayer = new ThroughPlaylistPlayer(this, CurrentPlaylist)
                {
                    IsRepeat = PlaylistSettings.RepeatMode == PlaybackMode.RepeatAll
                };
            }

            if (Player != null && Player.Playlist == CurrentPlaylist)
            {
                if (Player.Current != null)
                {
                    newPlayer.SetCurrent(Player.Current);
                }
            }

            Player = newPlayer;


            OnPropertyChanged(nameof(CanGoBack));
            OnPropertyChanged(nameof(CanGoNext));

            _CurrentPlaylistId = _CurrentPlaylist.Id;
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

            Player.SetCurrent(item);

            OpenPlaylistItem?.Invoke(CurrentPlaylist, item);

            IsDisplayPlayer = true;
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
            var playlist = new Playlist(id)
            {
                // TODO: Idの初期化
                Name = name,
                HohoemaPlaylist = this
            };

            Playlists.Add(playlist);

            return playlist;
        }

        public void RemovePlaylist(Playlist playlist)
        {
            if (Playlists.Contains(playlist))
            {
                Playlists.Remove(playlist);

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


        public PlaylistPlayerBase(HohoemaPlaylist parent, Playlist playlist)
        {
            Playlist = playlist;
            Current = Playlist.CurrentVideo;
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
        public ThroughPlaylistPlayer(HohoemaPlaylist parent, Playlist playlist)
            : base(parent, playlist)
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

        public ShufflePlaylistPlayer(HohoemaPlaylist parent, Playlist playlist)
            : base(parent, playlist)
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
        public RepeatOnePlaylistPlayer(HohoemaPlaylist parent, Playlist playlist)
            : base(parent, playlist)
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

        [DataMember]
        public string Id { get; private set; }

        private string _Name;

        [DataMember]
        public string Name
        {
            get { return _Name; }
            set { SetProperty(ref _Name, value); }
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

        public Playlist(string id)
            : this()
        {
            Id = id;

            _ItemsChangedObserveDisposer = PlaylistItems.CollectionChangedAsObservable()
                .Subscribe(x => 
                {
                    LastUpdate = DateTime.Now;
                });
        }


        public void Dispose()
        {
            _ItemsChangedObserveDisposer?.Dispose();
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
                Quality = quality
            };

            _PlaylistItems.Insert(0, newItem);

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
                    return true;
                }
            }

            return false;
        }

    }

    public class PlaylistItem
    {
        internal PlaylistItem() { }

        public PlaylistItemType Type { get; set; }
        public string ContentId { get; set; }
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
