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

namespace NicoPlayerHohoema.ViewModels
{
	public class CacheManagementPageViewModel : HohoemaViewModelBase
	{
		public static SynchronizationContextScheduler scheduler;
		public CacheManagementPageViewModel(HohoemaApp app, PageManager pageManager)
			: base(pageManager)
		{
			if (scheduler == null)
			{
				scheduler = new SynchronizationContextScheduler(SynchronizationContext.Current);
			}
			_HohoemaApp = app;
			_MediaManager = app.MediaManager;

			_CacheVideoViewModelSemaphore = new SemaphoreSlim(1, 1);

			_CacheVideoVMs = new Dictionary<NicoVideoCacheRequest, CacheVideoViewModel>();
			CacheVideoItems = new ObservableCollection<CacheVideoViewModel>();


		}

		public override async void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			base.OnNavigatedTo(e, viewModelState);

			await this.UpdateCacheItemsVM();
		}

		private async Task<CacheVideoViewModel> ToCacheVideoViewModel(string videoId, NicoVideoQuality quality)
		{
			return await ToCacheVideoViewModel(new NicoVideoCacheRequest()
			{
				RawVideoid = videoId,
				Quality = quality
			});
		}


		private async Task<CacheVideoViewModel> ToCacheVideoViewModel(NicoVideoCacheRequest req)
		{
			try
			{
				await _CacheVideoViewModelSemaphore.WaitAsync().ConfigureAwait(false);

				if (_CacheVideoVMs.ContainsKey(req))
				{
					return _CacheVideoVMs[req];
				}
				else
				{
					var nicoVideo = await _MediaManager.GetNicoVideo(req.RawVideoid);
					

					var vm = new CacheVideoViewModel(nicoVideo, req.Quality, PageManager);

					vm.LoadThumbnail();
					_CacheVideoVMs.Add(req, vm);

					return vm;
				}
			}
			finally
			{
				_CacheVideoViewModelSemaphore.Release();
			}
			
		}

		private async Task UpdateCacheItemsVM()
		{
			List<CacheVideoViewModel> list = new List<CacheVideoViewModel>();


			foreach (var item in _MediaManager.VideoIdToNicoVideo.Values)
			{
				await item.SetupVideoInfoFromLocal();
				await item.CheckCacheStatus();

				if (item.OriginalQualityCacheState != NicoVideoCacheState.Incomplete || 
					item.HasOriginalQualityIncompleteVideoFile()
					)
				{
					var vm = await ToCacheVideoViewModel(item.RawVideoId, NicoVideoQuality.Original);
					list.Add(vm);
				}

				if (item.LowQualityCacheState != NicoVideoCacheState.Incomplete ||
					item.HasLowQualityIncompleteVideoFile()
					)
				{
					var vm = await ToCacheVideoViewModel(item.RawVideoId, NicoVideoQuality.Low);
					list.Add(vm);
				}
			}

			CacheVideoItems.Clear();
			foreach (var vm in list.OrderBy(x => x.CacheRequestTime).Reverse())
			{
				CacheVideoItems.Add(vm);
			}
		}

		

		public override string GetPageTitle()
		{
			return "ダウンロード管理";
		}




		private SemaphoreSlim _CacheVideoViewModelSemaphore;

		HohoemaApp _HohoemaApp;
		NiconicoMediaManager _MediaManager;

		private Dictionary<NicoVideoCacheRequest, CacheVideoViewModel> _CacheVideoVMs;

		
		/// <summary>
		/// キャッシュ未完了のアイテムも含むキャッシュファイル
		/// </summary>
		public ObservableCollection<CacheVideoViewModel> CacheVideoItems { get; private set; }
	}

	// 自身のキャッシュ状況を表現する
	// キャッシュ処理の状態、進捗状況
	
	


	public class CacheVideoViewModel : VideoInfoControlViewModel
	{

		public CacheVideoViewModel(NicoVideo nicoVideo, NicoVideoQuality quality, PageManager pageManager)
			: base(nicoVideo, pageManager)
		{
			_CompositeDisposable = new CompositeDisposable();

			Quality = quality;
			CacheRequestTime = nicoVideo.CacheRequestTime;

			if (quality == NicoVideoQuality.Low)
			{
				IsIncompleteCache = nicoVideo.LowQualityCacheState != NicoVideoCacheState.Cached;
				ProgressPercent = nicoVideo.ObserveProperty(x => x.LowQualityCacheProgressSize)
					.Select(x => ProgressToPercent(x, nicoVideo.LowQualityVideoSize))
					.ToReactiveProperty(CacheManagementPageViewModel.scheduler)
					.AddTo(_CompositeDisposable);
				IsVisibleProgress = nicoVideo.ObserveProperty(x => x.LowQualityCacheState)
					.Select(x => x == NicoVideoCacheState.NowDownloading)
					.ToReactiveProperty(CacheManagementPageViewModel.scheduler)
					.AddTo(_CompositeDisposable);
				CacheState = nicoVideo.ObserveProperty(x => x.LowQualityCacheState)
					.ToReactiveProperty(CacheManagementPageViewModel.scheduler)
					.AddTo(_CompositeDisposable);

			}
			else
			{
				IsIncompleteCache = nicoVideo.OriginalQualityCacheState != NicoVideoCacheState.Cached;
				ProgressPercent = nicoVideo.ObserveProperty(x => x.OriginalQualityCacheProgressSize)
					.Select(x => ProgressToPercent(x, nicoVideo.OriginalQualityVideoSize))
					.ToReactiveProperty(CacheManagementPageViewModel.scheduler)
					.AddTo(_CompositeDisposable);
				IsVisibleProgress = nicoVideo.ObserveProperty(x => x.OriginalQualityCacheState)
					.Select(x => x == NicoVideoCacheState.NowDownloading)
					.ToReactiveProperty(CacheManagementPageViewModel.scheduler)
					.AddTo(_CompositeDisposable);
				CacheState = nicoVideo.ObserveProperty(x => x.OriginalQualityCacheState)
					.ToReactiveProperty(CacheManagementPageViewModel.scheduler)
					.AddTo(_CompositeDisposable);
			}


			
		}

		public virtual void Dispose()
		{
			_CompositeDisposable?.Dispose();
		}

		private float ProgressToPercent(uint size, uint totalSize)
		{
			if (size == totalSize) { return 100.0f; }
			return (float)Math.Floor((size / (float)totalSize) * 1000.0f) * 0.1f;
		}


		private CompositeDisposable _CompositeDisposable;

		public bool IsIncompleteCache { get; private set; }

		public DateTime CacheRequestTime { get; private set; }

		public NicoVideoQuality Quality { get; private set; }
		public ReactiveProperty<NicoVideoCacheState> CacheState { get; private set; }

		public ReactiveProperty<float> ProgressPercent { get; private set; }

		public ReactiveProperty<bool> IsVisibleProgress { get; private set; }

		

		
	}

}
