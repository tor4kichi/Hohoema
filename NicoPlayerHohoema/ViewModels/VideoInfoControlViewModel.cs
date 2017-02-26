using Mntone.Nico2;
using Mntone.Nico2.Mylist;
using Mntone.Nico2.Mylist.MylistGroup;
using Mntone.Nico2.Searches.Video;
using Mntone.Nico2.Videos.Ranking;
using Mntone.Nico2.Videos.Thumbnail;
using NicoPlayerHohoema.Models;
using NicoPlayerHohoema.Models.Db;
using Prism.Commands;
using Prism.Mvvm;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NicoPlayerHohoema.ViewModels
{
	public class VideoInfoControlViewModel : HohoemaListingPageItemBase
	{
	//	private IScheduler scheduler;


		

		// とりあえずマイリストから取得したデータによる初期化
		public VideoInfoControlViewModel(MylistData data, NicoVideo nicoVideo, PageManager pageManager)
			: this(nicoVideo, pageManager)
		{
			Title = data.Title;
			RawVideoId = data.ItemId;
            OptionText = data.CreateTime.ToString();
            if (!string.IsNullOrWhiteSpace(data.ThumbnailUrl.OriginalString))
            {
                ImageUrlsSource.Add(data.ThumbnailUrl.OriginalString);
            }

            if (!nicoVideo.IsDeleted)
            {
                Description = $"再生:{data.ViewCount}";
            }

			ImageCaption = data.Length.ToString(); // TODO: ユーザーフレンドリィ時間

			VideoId = RawVideoId;
		}


		// 個別マイリストから取得したデータによる初期化
		public VideoInfoControlViewModel(VideoInfo data, NicoVideo nicoVideo, PageManager pageManager)
			: this(nicoVideo, pageManager)
		{
            
            Title = data.Video.Title;
            RawVideoId = data.Video.Id;
            OptionText = data.Video.UploadTime.ToString();
            if (!string.IsNullOrWhiteSpace(data.Video.ThumbnailUrl.OriginalString))
            {
                ImageUrlsSource.Add(data.Video.ThumbnailUrl.OriginalString);
            }

            if (!nicoVideo.IsDeleted)
            {
                Description = $"再生:{data.Video.ViewCount}";
            }

            ImageCaption = data.Video.Length.ToString(); // TODO: ユーザーフレンドリィ時間

            VideoId = RawVideoId;
        }


		public VideoInfoControlViewModel(NicoVideo nicoVideo, PageManager pageManager)
		{
			PageManager = pageManager;
            HohoemaPlaylist = nicoVideo.HohoemaApp.Playlist;
            NicoVideo = nicoVideo;
			_CompositeDisposable = new CompositeDisposable();

			Title = nicoVideo.Title;
			RawVideoId = nicoVideo.RawVideoId;
			VideoId = nicoVideo.VideoId;

            if (nicoVideo.IsDeleted)
            {
                Description = "Deleted";
            }

            IsRequireConfirmDelete = new ReactiveProperty<bool>(nicoVideo.IsRequireConfirmDelete);
            PrivateReasonText = nicoVideo.PrivateReasonType.ToString() ?? "";

            SetupFromThumbnail(nicoVideo);

            QualityDividedVideos = new ObservableCollection<QualityDividedNicoVideoListItemViewModel>();
            NicoVideo.QualityDividedVideos.CollectionChangedAsObservable()
                .Subscribe(_ => ResetQualityDivideVideosVM())
                .AddTo(_CompositeDisposable);

            if (QualityDividedVideos.Count == 0 && NicoVideo.QualityDividedVideos.Count != 0)
            {
                ResetQualityDivideVideosVM();
            }


            IsCacheRequested = NicoVideo.QualityDividedVideos
                .ObserveElementProperty(x => x.IsCacheRequested)
                .Select(x => NicoVideo.QualityDividedVideos.Any(y => y.IsCacheRequested))
                .ToReadOnlyReactiveProperty()
                .AddTo(_CompositeDisposable);
        }

        private async void ResetQualityDivideVideosVM()
        {
            using (var releaser = await _QualityDividedVideosLock.LockAsync())
            {
                QualityDividedVideos.Clear();

                foreach (var div in NicoVideo.QualityDividedVideos.ToArray())
                {
                    var vm = new QualityDividedNicoVideoListItemViewModel(div, HohoemaPlaylist)
                        .AddTo(_CompositeDisposable);
                    QualityDividedVideos.Add(vm);
                }

            }
        }

		public void SetupFromThumbnail(NicoVideo info)
		{
			// NG判定
			var ngResult = NicoVideo.CheckUserNGVideo();
			IsVisible = ngResult != null;

            Title = info.Title;
            OptionText = info.PostedAt.ToString("yyyy/MM/dd HH:mm");
            if (!string.IsNullOrWhiteSpace(info.ThumbnailUrl))
            {
                ImageUrlsSource.Add(info.ThumbnailUrl);
            }

            if (!info.IsDeleted)
            {
                Description = $"再生:{info.ViewCount.ToString("N0")} コメ:{info.CommentCount.ToString("N0")} マイ:{info.MylistCount.ToString("N0")}";
            }

            string timeText;
            if (info.VideoLength.Hours > 0)
            {
                timeText = info.VideoLength.ToString(@"hh\:mm\:ss");
            }
            else
            {
                timeText = info.VideoLength.ToString(@"mm\:ss");
            }
            ImageCaption = timeText;
        }


		
		protected override void OnDispose()
		{
			_CompositeDisposable?.Dispose();
		}



		protected virtual VideoPlayPayload MakeVideoPlayPayload()
		{
			return new VideoPlayPayload()
			{
				VideoId = RawVideoId,
				Quality = null,
			};
		}

       
		
		public string VideoId { get; private set; }

		public string RawVideoId { get; private set; }

        public VideoStatus VideoStatus { get; private set; }


		public override ICommand PrimaryCommand
		{
			get
			{
				return PlayCommand;
			}
		}


		private DelegateCommand _PlayCommand;
		public DelegateCommand PlayCommand
		{
			get
			{
				return _PlayCommand
					?? (_PlayCommand = new DelegateCommand(() =>
					{
                        HohoemaPlaylist.PlayVideo(RawVideoId, Title);

//                        var payload = MakeVideoPlayPayload();
//						PageManager.OpenPage(HohoemaPageType.VideoPlayer, payload.ToParameterString());
					}));
			}
		}


        private DelegateCommand _OpenVideoInfoPageCommand;
        public DelegateCommand OpenVideoInfoPageCommand
        {
            get
            {
                return _OpenVideoInfoPageCommand
                    ?? (_OpenVideoInfoPageCommand = new DelegateCommand(() =>
                    {
                        var videoId = VideoId != null ? VideoId : RawVideoId;
                        PageManager.OpenPage(HohoemaPageType.VideoInfomation, videoId);
                    }));
            }
        }

        private DelegateCommand _CacheRequestCommand;
        public DelegateCommand CacheRequestCommand
        {
            get
            {
                return _CacheRequestCommand
                    ?? (_CacheRequestCommand = new DelegateCommand(() =>
                    {
                        if (NicoVideo.IsOriginalQualityOnly)
                        {
                            NicoVideo.RequestCache(NicoVideoQuality.Original);
                        }
                        else
                        {
                            if (NicoVideo.HohoemaApp.UserSettings.PlayerSettings.IsLowQualityDeafult)
                            {
                                NicoVideo.RequestCache(NicoVideoQuality.Low);
                            }
                            else
                            {
                                try
                                {
                                    NicoVideo.RequestCache(NicoVideoQuality.Original);
                                }
                                catch
                                {
                                    NicoVideo.RequestCache(NicoVideoQuality.Low);
                                }
                            }
                        }
                        
                    }));
            }
        }




        private DelegateCommand _PlayWithSmallPlayerCommand;
        public DelegateCommand PlayWithSmallPlayerCommand
        {
            get
            {
                return _PlayWithSmallPlayerCommand
                    ?? (_PlayWithSmallPlayerCommand = new DelegateCommand(() =>
                    {
                        HohoemaPlaylist.IsPlayerFloatingModeEnable = true;
                        HohoemaPlaylist.PlayVideo(RawVideoId, Title);
                    }));
            }
        }
        


        private DelegateCommand _RemoveCacheCommand;
        public DelegateCommand RemoveCacheCommand
        {
            get
            {
                return _RemoveCacheCommand
                    ?? (_RemoveCacheCommand = new DelegateCommand(async () =>
                    {
                        await NicoVideo.CancelCacheRequest();
                    }));
            }
        }


        private DelegateCommand _AddDefaultPlaylistCommand;
        public DelegateCommand AddDefaultPlaylistCommand
        {
            get
            {
                return _AddDefaultPlaylistCommand
                    ?? (_AddDefaultPlaylistCommand = new DelegateCommand(() =>
                    {
                        var hohoemaApp = NicoVideo.HohoemaApp;
                        hohoemaApp.Playlist.DefaultPlaylist.AddVideo(this.RawVideoId, this.Title);
                    }));
            }
        }

        private DelegateCommand _OpenOwnerVideoListPageCommand;
        public DelegateCommand OpenOwnerVideoListPageCommand
        {
            get
            {
                return _OpenOwnerVideoListPageCommand
                    ?? (_OpenOwnerVideoListPageCommand = new DelegateCommand(() =>
                    {
                        PageManager.OpenPage(HohoemaPageType.UserVideo, this.NicoVideo.VideoOwnerId.ToString());
                    }));
            }
        }


        private DelegateCommand _ConfirmDeleteCommand;
        public DelegateCommand ConfirmDeleteCommand
        {
            get
            {
                return _ConfirmDeleteCommand
                    ?? (_ConfirmDeleteCommand = new DelegateCommand(() =>
                    {
                        try
                        {
                            // TODO: MediaManagerに削除動画の確認が済んだことを伝える
                            //							NicoVideo.DeletedVideoConfirmedFromUser(NicoVideo).ConfigureAwait(false);
                            IsRequireConfirmDelete.Value = false;
                        }
                        catch { }
                    }));
            }
        }


        public ReadOnlyReactiveProperty<bool> IsCacheRequested { get; private set; }

        public ReactiveProperty<bool> IsRequireConfirmDelete { get; private set; }
        public string PrivateReasonText { get; private set; }


        private Util.AsyncLock _QualityDividedVideosLock = new Util.AsyncLock();
        public ObservableCollection<QualityDividedNicoVideoListItemViewModel> QualityDividedVideos { get; private set; }

        protected CompositeDisposable _CompositeDisposable { get; private set; }

		public NicoVideo NicoVideo { get; private set; }
        public HohoemaPlaylist HohoemaPlaylist { get; private set; }
        public PageManager PageManager { get; private set; }
	}


    public class QualityDividedNicoVideoListItemViewModel  : BindableBase, IDisposable
    {
        public HohoemaPlaylist HohoemaPlaylist { get; private set; }
        public DividedQualityNicoVideo DividedQualityNicoVideo { get; private set; }

        public NicoVideoQuality Quality { get; private set; }

        public ReadOnlyReactiveProperty<NicoVideoCacheState> CacheState { get; private set; }

        public ReadOnlyReactiveProperty<bool> IsNotCacheRequested { get; private set; }
        public ReadOnlyReactiveProperty<bool> IsCachePending { get; private set; }
        public ReadOnlyReactiveProperty<bool> IsCacheDownloading { get; private set; }
        public ReadOnlyReactiveProperty<bool> IsCached { get; private set; }

        public ReactiveProperty<float> ProgressPercent { get; private set; }

        IDisposable _ProgressParcentageMoniterDisposer;


        private CompositeDisposable _CompositeDisposable = new CompositeDisposable();

        public QualityDividedNicoVideoListItemViewModel(DividedQualityNicoVideo divQualityVideo, HohoemaPlaylist playlist)
        {
            DividedQualityNicoVideo = divQualityVideo;
            HohoemaPlaylist = playlist;
            Quality = DividedQualityNicoVideo.Quality;

            CacheState = DividedQualityNicoVideo.ObserveProperty(x => x.CacheState)
                .ToReadOnlyReactiveProperty()
                .AddTo(_CompositeDisposable);

            IsNotCacheRequested = CacheState.Select(x => x == NicoVideoCacheState.NotCacheRequested)
                .ToReadOnlyReactiveProperty()
                .AddTo(_CompositeDisposable);
            IsCachePending = CacheState.Select(x => x == NicoVideoCacheState.Pending)
                .ToReadOnlyReactiveProperty()
                .AddTo(_CompositeDisposable);
            IsCacheDownloading = CacheState.Select(x => x == NicoVideoCacheState.Downloading)
                .ToReadOnlyReactiveProperty()
                .AddTo(_CompositeDisposable);
            IsCached = CacheState.Select(x => x == NicoVideoCacheState.Cached)
                .ToReadOnlyReactiveProperty()
                .AddTo(_CompositeDisposable);


            ProgressPercent = new ReactiveProperty<float>(DividedQualityNicoVideo.IsCached ? 100.0f : 0.0f);

            CacheState.Subscribe(x =>
            {
                if (x == NicoVideoCacheState.Downloading)
                {
                    _ProgressParcentageMoniterDisposer?.Dispose();
                    _ProgressParcentageMoniterDisposer =
                        DividedQualityNicoVideo.ObserveProperty(y => y.CacheProgressSize)
                        .Subscribe(y =>
                        {
                            ProgressPercent.Value = DividedQualityNicoVideo.GetDonwloadProgressParcentage();
                        });
                }
                else
                {
                    _ProgressParcentageMoniterDisposer?.Dispose();
                    _ProgressParcentageMoniterDisposer = null;
                }
            });
        }

        public void Dispose()
        {
            _CompositeDisposable?.Dispose();
            _CompositeDisposable = null;

            _ProgressParcentageMoniterDisposer?.Dispose();
        }


        private DelegateCommand _PlayCommand;
        public DelegateCommand PlayCommand
        {
            get
            {
                return _PlayCommand
                    ?? (_PlayCommand = new DelegateCommand(() =>
                    {
                        HohoemaPlaylist.PlayVideo(DividedQualityNicoVideo.RawVideoId, DividedQualityNicoVideo.NicoVideo.Title, Quality);

                        //                        var payload = MakeVideoPlayPayload();
                        //						PageManager.OpenPage(HohoemaPageType.VideoPlayer, payload.ToParameterString());
                    }));
            }
        }

        
    }


    [Flags]
    public enum VideoStatus
    {
        Watched = 0x0001,
        Filtered = 0x1000,
    }
}
