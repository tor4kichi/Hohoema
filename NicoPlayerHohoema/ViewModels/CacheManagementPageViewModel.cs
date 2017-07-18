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
					?? (_ResumeCacheCommand = new DelegateCommand(() =>
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
        public ReadOnlyReactiveProperty<DateTime> CacheRequestTime { get; private set; }

        public CacheVideoViewModel(NicoVideo nicoVideo, PageManager pageManager)
			: base(nicoVideo, pageManager, isNgEnabled:false)
		{
            CacheRequestTime = nicoVideo.ObserveProperty(x => x.CachedAt)
                .ToReadOnlyReactiveProperty()
                .AddTo(_CompositeDisposable);
		}

        protected override VideoPlayPayload MakeVideoPlayPayload()
		{
			var payload = base.MakeVideoPlayPayload();

//			payload.Quality = Quality;

			return payload;
		}


		
		
    }


	public class CacheVideoInfoLoadingSource : HohoemaVideoPreloadingIncrementalSourceBase<CacheVideoViewModel>
	{
		
		HohoemaApp _HohoemaApp;
		PageManager _PageManager;


		public List<NicoVideo> RawList { get; private set; }


		public CacheVideoInfoLoadingSource(HohoemaApp app, PageManager pageManager)
            : base(app, nameof(CacheManagementPageViewModel))
		{
			_HohoemaApp = app;
			_PageManager = pageManager;
		}

        protected override async Task<IEnumerable<NicoVideo>> PreloadNicoVideo(int start, int count)
        {
            await Task.Delay(0);
            return RawList.Skip(start).Take(count).ToList();
        }


        protected override async Task<int> HohoemaPreloadingResetSourceImpl()
        {
            // 
            var contentFinder = _HohoemaApp.ContentFinder;
            var mediaManager = _HohoemaApp.MediaManager;

            while (!mediaManager.IsInitialized)
            {
                await Task.Delay(50).ConfigureAwait(false);
            }

            var list = new List<NicoVideo>();

            foreach (var item in mediaManager.CacheVideos.ToArray())
            {
                if (item.GetAllQuality().ToArray().Any(x => x.IsCacheRequested))
                {
                    list.Add(item);
                }
            }

            RawList = list.OrderBy(x => x.CachedAt.Ticks).Reverse().ToList();

            return RawList.Count;
        }




        protected override CacheVideoViewModel NicoVideoToTemplatedItem(
            NicoVideo itemSource
            , int index
            )
        {
            return new CacheVideoViewModel(
                    itemSource
                    , _PageManager
                );
        }

        





		


		
	}

}
