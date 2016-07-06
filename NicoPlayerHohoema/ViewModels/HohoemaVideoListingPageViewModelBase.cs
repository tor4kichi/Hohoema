using NicoPlayerHohoema.Models;
using NicoPlayerHohoema.Util;
using Prism.Commands;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Windows.Navigation;

namespace NicoPlayerHohoema.ViewModels
{
	abstract public class HohoemaVideoListingPageViewModelBase<VIDEO_INFO_VM> : HohoemaViewModelBase
		where VIDEO_INFO_VM : VideoInfoControlViewModel
	{
		public HohoemaVideoListingPageViewModelBase(HohoemaApp app, PageManager pageManager)
			: base(pageManager)
		{
			HohoemaApp = app;

			SelectedVideoInfoItems = new ObservableCollection<VIDEO_INFO_VM>();

			ListViewVerticalOffset = new ReactiveProperty<double>(0.0);
			_LastListViewOffset = 0;

			var SelectionItemsChanged = SelectedVideoInfoItems.ToCollectionChanged().ToUnit();


			SelectedVideoInfoItems.CollectionChangedAsObservable()
				.Subscribe(x => 
				{
					Debug.WriteLine("Selected Count: " + SelectedVideoInfoItems.Count);
				});

			CancelCacheDownloadRequest = SelectionItemsChanged
				.Select(_ => EnumerateDownloadingVideoItems().Count() > 0)
				.ToReactiveCommand();

			CancelCacheDownloadRequest.Subscribe(_ => 
			{
				foreach (var item in EnumerateDownloadingVideoItems())
				{
					item.NicoVideo.CancelCacheRequest();
				}

				ClearSelection();
				UpdateList();
			});

			RequestOriginalQualityCacheDownload = SelectionItemsChanged
				.Select(_ => EnumerateCanDownloadVideoItem(NicoVideoQuality.Original).Count() > 0)
				.ToReactiveCommand();
			RequestOriginalQualityCacheDownload.Subscribe(async _ => 
			{
				foreach (var item in EnumerateCanDownloadVideoItem(NicoVideoQuality.Original))
				{
					await item.NicoVideo.RequestCache(NicoVideoQuality.Original);
				}

				ClearSelection();
				UpdateList();
			});

			RequestLowQualityCacheDownload = SelectionItemsChanged
				.Select(_ => EnumerateCanDownloadVideoItem(NicoVideoQuality.Low).Count() > 0)
				.ToReactiveCommand();
			RequestLowQualityCacheDownload.Subscribe(async _ =>
			{
				foreach (var item in EnumerateCanDownloadVideoItem(NicoVideoQuality.Low))
				{
					await item.NicoVideo.RequestCache(NicoVideoQuality.Low);
				}

				ClearSelection();
				UpdateList();

			});

			DeleteOriginalQualityCache = SelectionItemsChanged
				.Select(_ => EnumerateCachedVideoItem(NicoVideoQuality.Original).Count() > 0)
				.ToReactiveCommand();
			DeleteOriginalQualityCache.Subscribe(async _ =>
			{
				foreach (var item in EnumerateCachedVideoItem(NicoVideoQuality.Original))
				{
					await item.NicoVideo.DeleteCache(NicoVideoQuality.Original);
				}

				ClearSelection();
				ResetList();
			});

			DeleteLowQualityCache = SelectionItemsChanged
				.Select(_ => EnumerateCachedVideoItem(NicoVideoQuality.Low).Count() > 0)
				.ToReactiveCommand();
			DeleteLowQualityCache.Subscribe(async _ =>
			{
				foreach (var item in EnumerateCachedVideoItem(NicoVideoQuality.Low))
				{
					await item.NicoVideo.DeleteCache(NicoVideoQuality.Low);
				}

				ClearSelection();
				ResetList();
			});
		}

		
		public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			base.OnNavigatedTo(e, viewModelState);

			if (CheckNeedUpdate())
			{
				ResetList();
			}
			else
			{
				ListViewVerticalOffset.Value = _LastListViewOffset;
				ChangeCanIncmentalLoading(true);
			}
		}

		public override void OnNavigatingFrom(NavigatingFromEventArgs e, Dictionary<string, object> viewModelState, bool suspending)
		{
			base.OnNavigatingFrom(e, viewModelState, suspending);

			_LastListViewOffset = ListViewVerticalOffset.Value;
			ChangeCanIncmentalLoading(false);
		}


		private void ChangeCanIncmentalLoading(bool enableLoading)
		{
			if (IncrementalLoadingItems != null)
			{
//				IncrementalLoadingItems.IsPuaseLoading = !enableLoading;
			}
		}

		protected virtual void UpdateList()
		{
			// TODO: 表示中のアイテムすべての状態を更新
			// 主にキャッシュ状態の更新が目的
		}

		protected virtual void ResetList()
		{
			try
			{
				var source = GenerateIncrementalSource();

				IncrementalLoadingItems = new IncrementalLoadingCollection<IIncrementalSource<VIDEO_INFO_VM>, VIDEO_INFO_VM>(source, IncrementalLoadCount);

				PostResetList();
			}
			catch
			{
				IncrementalLoadingItems = null;

				Debug.WriteLine("failed GenerateIncrementalSource.");
			}
		}

		protected virtual void PostResetList() { }

		protected abstract uint IncrementalLoadCount { get; }

		abstract protected IIncrementalSource<VIDEO_INFO_VM> GenerateIncrementalSource();

		abstract protected bool CheckNeedUpdate();

		private IEnumerable<VideoInfoControlViewModel> EnumerateDownloadingVideoItems()
		{
			return SelectedVideoInfoItems.Where(x =>
			{
				return 
					x.NicoVideo.OriginalQualityCacheState == NicoVideoCacheState.NowDownloading
					|| x.NicoVideo.OriginalQualityCacheState == NicoVideoCacheState.CacheRequested
					|| x.NicoVideo.LowQualityCacheState == NicoVideoCacheState.NowDownloading
					|| x.NicoVideo.LowQualityCacheState == NicoVideoCacheState.CacheRequested;
			});
		}

		private IEnumerable<VideoInfoControlViewModel> EnumerateCanDownloadVideoItem(NicoVideoQuality quality)
		{
			switch (quality)
			{
				case NicoVideoQuality.Original:
					return SelectedVideoInfoItems.Where(x => x.NicoVideo.CanRequestDownloadOriginalQuality);
				case NicoVideoQuality.Low:
					return SelectedVideoInfoItems.Where(x => x.NicoVideo.CanRequestDownloadLowQuality);
				default:
					return Enumerable.Empty<VideoInfoControlViewModel>();
			}
		}

		private IEnumerable<VideoInfoControlViewModel> EnumerateCachedVideoItem(NicoVideoQuality quality)
		{
			var qualityFilterdVideoItems = SelectedVideoInfoItems
				.Where(x =>
				{
					if (x is CacheVideoViewModel)
					{
						var cacheVideoVM = x as CacheVideoViewModel;
						return cacheVideoVM.Quality == quality;
					}
					return true;
				});
			switch (quality)
			{
				case NicoVideoQuality.Original:
					return qualityFilterdVideoItems
						.Where(x => x.NicoVideo.OriginalQualityCacheState != NicoVideoCacheState.Incomplete || x.NicoVideo.HasOriginalQualityIncompleteVideoFile());
				case NicoVideoQuality.Low:
					return qualityFilterdVideoItems
						.Where(x => x.NicoVideo.LowQualityCacheState != NicoVideoCacheState.Incomplete || x.NicoVideo.HasLowQualityIncompleteVideoFile());
				default:
					return Enumerable.Empty<VideoInfoControlViewModel>();
			}
		}



		private void ClearSelection()
		{
			SelectedVideoInfoItems.Clear();
		}




		public ObservableCollection<VIDEO_INFO_VM> SelectedVideoInfoItems { get; private set; }

		public IncrementalLoadingCollection<IIncrementalSource<VIDEO_INFO_VM>, VIDEO_INFO_VM> IncrementalLoadingItems { get; private set; }

		public ReactiveProperty<double> ListViewVerticalOffset { get; private set; }
		private double _LastListViewOffset;


		public ReactiveCommand CancelCacheDownloadRequest { get; private set; }
		public ReactiveCommand RequestOriginalQualityCacheDownload { get; private set; }
		public ReactiveCommand RequestLowQualityCacheDownload { get; private set; }
		public ReactiveCommand DeleteOriginalQualityCache { get; private set; }
		public ReactiveCommand DeleteLowQualityCache { get; private set; }


		public HohoemaApp HohoemaApp { get; private set; }


	}
}
