using NicoPlayerHohoema.Models;
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

namespace NicoPlayerHohoema.ViewModels
{
	abstract public class HohoemaVideoListingPageViewModelBase : HohoemaViewModelBase
	{
		public HohoemaVideoListingPageViewModelBase(HohoemaApp app, PageManager pageManager)
			: base(pageManager)
		{
			HohoemaApp = app;

			SelectedVideoInfoItems = new ObservableCollection<VideoInfoControlViewModel>();

			var SelectionItemsChanged = SelectedVideoInfoItems.ToCollectionChanged().ToUnit();

			CancelCacheDownloadRequest = SelectionItemsChanged
				.Select(_ => EnumerateDownloadingVideoItems().Count() > 0)
				.ToReactiveCommand();

			CancelCacheDownloadRequest.Subscribe(_ => 
			{
				foreach (var item in EnumerateDownloadingVideoItems())
				{
					item.NicoVideo.CancelCacheRequest();
				}

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

				UpdateList();
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

				UpdateList();
			});
		}


		abstract protected void UpdateList();

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
			switch (quality)
			{
				case NicoVideoQuality.Original:
					return SelectedVideoInfoItems.Where(x => x.NicoVideo.OriginalQualityCacheState != NicoVideoCacheState.Incomplete || x.NicoVideo.HasOriginalQualityIncompleteVideoFile());
				case NicoVideoQuality.Low:
					return SelectedVideoInfoItems.Where(x => x.NicoVideo.LowQualityCacheState != NicoVideoCacheState.Incomplete || x.NicoVideo.HasLowQualityIncompleteVideoFile());
				default:
					return Enumerable.Empty<VideoInfoControlViewModel>();
			}
		}


		public ObservableCollection<VideoInfoControlViewModel> SelectedVideoInfoItems { get; private set; }

		public ReactiveCommand CancelCacheDownloadRequest { get; private set; }
		public ReactiveCommand RequestOriginalQualityCacheDownload { get; private set; }
		public ReactiveCommand RequestLowQualityCacheDownload { get; private set; }
		public ReactiveCommand DeleteOriginalQualityCache { get; private set; }
		public ReactiveCommand DeleteLowQualityCache { get; private set; }



		public HohoemaApp HohoemaApp { get; private set; }


	}
}
