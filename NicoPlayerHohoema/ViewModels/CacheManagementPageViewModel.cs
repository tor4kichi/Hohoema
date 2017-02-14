using Mntone.Nico2.Videos.WatchAPI;
using NicoPlayerHohoema.Models;
using Prism.Mvvm;
using Prism.Windows.Mvvm;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Prism.Windows.Navigation;
using System.Threading;
using System.Reactive.Disposables;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Reactive.Concurrency;
using NicoPlayerHohoema.Util;
using Windows.UI.Xaml.Navigation;
using Prism.Commands;
using Windows.Networking.BackgroundTransfer;

namespace NicoPlayerHohoema.ViewModels
{
	public class CacheManagementPageViewModel : HohoemaVideoListingPageViewModelBase<CacheVideoViewModel>
	{
		public static SynchronizationContextScheduler scheduler;
		public CacheManagementPageViewModel(HohoemaApp app, PageManager pageManager, Views.Service.MylistRegistrationDialogService mylistDialogService)
			: base(app, pageManager, mylistDialogService)
		{
			if (scheduler == null)
			{
				scheduler = new SynchronizationContextScheduler(SynchronizationContext.Current);
			}
			_MediaManager = app.MediaManager;

			OpenCacheSettingsCommand = new DelegateCommand(() => 
			{
				PageManager.OpenPage(HohoemaPageType.Settings, HohoemaSettingsKind.Cache.ToString());
			});
		}


        #region Implement HohoemaVideListViewModelBase

        public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
        {
            IsCacheUserAccepted = HohoemaApp.UserSettings.CacheSettings.IsUserAcceptedCache;

            base.OnNavigatedTo(e, viewModelState);
        }

        protected override IIncrementalSource<CacheVideoViewModel> GenerateIncrementalSource()
		{
			return new CacheVideoInfoLoadingSource(HohoemaApp, PageManager);
		}

		protected override bool CheckNeedUpdateOnNavigateTo(NavigationMode mode)
		{
			return true;
		}

		protected override void PostResetList()
		{
			
		}




        #endregion


        private bool _IsCacheUserAccepted;
        public bool IsCacheUserAccepted
        {
            get { return _IsCacheUserAccepted; }
            set { SetProperty(ref _IsCacheUserAccepted, value); }
        }


		private DelegateCommand _ResumeCacheCommand;
		public DelegateCommand ResumeCacheCommand
		{
			get
			{
				return _ResumeCacheCommand
					?? (_ResumeCacheCommand = new DelegateCommand(async () =>
					{
                        // TODO: バックグラウンドダウンロードの強制更新？
						//await _MediaManager.StartBackgroundDownload();
					}));
			}
		}


		public DelegateCommand OpenCacheSettingsCommand { get; private set; }

		NiconicoMediaManager _MediaManager;
	}

	// 自身のキャッシュ状況を表現する
	// キャッシュ処理の状態、進捗状況
	
	
	
	public class CacheVideoViewModel : VideoInfoControlViewModel
	{


        public string PrivateReasonText { get; private set; }


        public bool IsIncompleteCache { get; private set; }

        public DateTime CacheRequestTime { get; private set; }

        public NicoVideoQuality Quality { get; private set; }
        public ReactiveProperty<NicoVideoCacheState> CacheState { get; private set; }

        public ReactiveProperty<bool> IsRequireConfirmDelete { get; private set; }

        public DividedQualityNicoVideo DividedQualityNicoVideo { get; private set; }

        public ReactiveProperty<float> ProgressPercent { get; private set; }
        public ReactiveProperty<bool> IsVisibleProgress { get; private set; }
        public ReactiveProperty<BackgroundTransferStatus> TransferStatus { get; private set; }

        Util.AsyncLock _ProgressMonitorLock = new Util.AsyncLock();
        IDisposable _ProgressParcentageMoniterDisposer;

        public CacheVideoViewModel(NicoVideo nicoVideo, NicoVideoQuality quality, PageManager pageManager)
			: base(nicoVideo, pageManager)
		{
			Quality = quality;

            DividedQualityNicoVideo = nicoVideo.GetDividedQualityNicoVideo(quality);

            IsIncompleteCache = !DividedQualityNicoVideo.IsCached;
			CacheState = DividedQualityNicoVideo.ObserveProperty(x => x.CacheState)
				.ToReactiveProperty(CacheManagementPageViewModel.scheduler)
				.AddTo(_CompositeDisposable);

			CacheRequestTime = DividedQualityNicoVideo.VideoFileCreatedAt;

            ProgressPercent = new ReactiveProperty<float>(DividedQualityNicoVideo.IsCached ? 100.0f : 0.0f);
            IsVisibleProgress = CacheState
                .Select(x => x == NicoVideoCacheState.Downloading)
                .ToReactiveProperty();


            TransferStatus = new ReactiveProperty<BackgroundTransferStatus>(BackgroundTransferStatus.Idle);

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

            PrivateReasonText = nicoVideo.PrivateReasonType.ToString() ?? "";
			IsRequireConfirmDelete = new ReactiveProperty<bool>(nicoVideo.IsRequireConfirmDelete);
		}


        protected override async void OnDispose()
        {
            using (var releaser = await _ProgressMonitorLock.LockAsync())
            {
                _ProgressParcentageMoniterDisposer?.Dispose();
            }

            base.OnDispose();
        }

        protected override VideoPlayPayload MakeVideoPlayPayload()
		{
			var payload = base.MakeVideoPlayPayload();

			payload.Quality = Quality;

			return payload;
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

		
    }


	public class CacheVideoInfoLoadingSource : IIncrementalSource<CacheVideoViewModel>
	{
		
		HohoemaApp _HohoemaApp;
		PageManager _PageManager;


		public List<CacheVideoViewModel> RawList { get; private set; }

		public uint OneTimeLoadCount
		{
			get
			{
				return 10;
			}
		}



		public CacheVideoInfoLoadingSource(HohoemaApp app, PageManager pageManager)
		{
			_HohoemaApp = app;
			_PageManager = pageManager;
		}

		public async Task<int> ResetSource()
		{
			List<CacheVideoViewModel> list = new List<CacheVideoViewModel>();

			// 
			var contentFinder = _HohoemaApp.ContentFinder;
			var mediaManager = _HohoemaApp.MediaManager;

            while (!mediaManager.IsInitialized)
            {
                await Task.Delay(50).ConfigureAwait(false);
            }


            foreach (var item in mediaManager.CacheVideos.ToArray())
			{
                foreach (var divided in item.GetAllQuality())
                {
                    if (divided.IsCacheRequested)
                    {
                        var vm = await ToCacheVideoViewModel(divided.RawVideoId, divided.Quality);
                        list.Add(vm);
                    }
                }
			}

			RawList = list.OrderBy(x => x.CacheRequestTime).Reverse().ToList();

			return RawList.Count;
		}

		public Task<IEnumerable<CacheVideoViewModel>> GetPagedItems(int head, int count)
		{
			return Task.FromResult(RawList.Skip(head).Take((int)count));
		}





		private async Task<CacheVideoViewModel> ToCacheVideoViewModel(string videoId, NicoVideoQuality quality)
		{
			return await ToCacheVideoViewModel(new NicoVideoCacheRequest()
			{
				RawVideoId = videoId,
				Quality = quality
			});
		}


		private async Task<CacheVideoViewModel> ToCacheVideoViewModel(NicoVideoCacheRequest req)
		{
			var mediaManager = _HohoemaApp.MediaManager;

			var nicoVideo = await mediaManager.GetNicoVideoAsync(req.RawVideoId);

			var vm = new CacheVideoViewModel(nicoVideo, req.Quality, _PageManager);

			return vm;

		}




		
	}

}
