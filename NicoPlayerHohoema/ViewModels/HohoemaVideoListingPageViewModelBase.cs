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
using Windows.UI.Xaml.Controls;

namespace NicoPlayerHohoema.ViewModels
{
	abstract public class HohoemaVideoListingPageViewModelBase<VIDEO_INFO_VM> : HohoemaViewModelBase
		where VIDEO_INFO_VM : VideoInfoControlViewModel
	{
		public HohoemaVideoListingPageViewModelBase(HohoemaApp app, PageManager pageManager)
			: base(pageManager)
		{
			HohoemaApp = app;

			NowLoadingItems = new ReactiveProperty<bool>(false);
			SelectedVideoInfoItems = new ObservableCollection<VIDEO_INFO_VM>();

			ListViewVerticalOffset = new ReactiveProperty<double>(0.0);
			_LastListViewOffset = 0;

			// 複数選択モード
			IsSelectionModeEnable = new ReactiveProperty<bool>(false);
			SelectionMode = IsSelectionModeEnable
				.Select(x => x ? ListViewSelectionMode.Multiple : ListViewSelectionMode.None)
				.ToReactiveProperty();


			// 複数選択モードによって再生コマンドの呼び出しを制御する
			PlayCommand = IsSelectionModeEnable
				.Select(x => !x)
				.ToReactiveCommand<VideoInfoControlViewModel>();

			PlayCommand.Subscribe(x => x?.PlayCommand.Execute());


			var SelectionItemsChanged = SelectedVideoInfoItems.ToCollectionChanged().ToUnit();

#if DEBUG
			SelectedVideoInfoItems.CollectionChangedAsObservable()
				.Subscribe(x => 
				{
					Debug.WriteLine("Selected Count: " + SelectedVideoInfoItems.Count);
				});
#endif


			PlayAllCommand = SelectionItemsChanged
				.Select(_ => SelectedVideoInfoItems.Count > 0)
				.ToReactiveCommand(false);

			PlayAllCommand.Subscribe(_ => 
			{

				// TODO: プレイリストに登録
				// プレイリストを空にしてから選択動画を登録

				SelectedVideoInfoItems.First()?.PlayCommand.Execute();
			});

			CancelCacheDownloadRequest = SelectionItemsChanged
				.Select(_ => EnumerateDownloadingVideoItems().Count() > 0)
				.ToReactiveCommand(false);

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
				.ToReactiveCommand(false);
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
				.ToReactiveCommand(false);
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
				.ToReactiveCommand(false);
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
				.ToReactiveCommand(false);
			DeleteLowQualityCache.Subscribe(async _ =>
			{
				foreach (var item in EnumerateCachedVideoItem(NicoVideoQuality.Low))
				{
					await item.NicoVideo.DeleteCache(NicoVideoQuality.Low);
				}

				ClearSelection();
				ResetList();
			});

			// クオリティ指定無しのキャッシュDLリクエスト
			RequestCacheDownload = SelectionItemsChanged
				.Select(_ => EnumerateCanDownloadVideoItem(NicoVideoQuality.Low).Count() > 0)
				.ToReactiveCommand(false);

			RequestCacheDownload.Subscribe(async _ =>
			{
				foreach (var item in EnumerateCanDownloadVideoItem(NicoVideoQuality.Low))
				{
					await item.NicoVideo.RequestCache(NicoVideoQuality.Low);
				}

				ClearSelection();
				UpdateList();
			});


			// クオリティ指定無しのキャッシュ削除
			DeleteCache = SelectionItemsChanged
				.Select(_ => EnumerateCachedVideoItem(NicoVideoQuality.Low).Count() > 0 || EnumerateCachedVideoItem(NicoVideoQuality.Original).Count() > 0)
				.ToReactiveCommand(false);
			DeleteCache.Subscribe(async _ =>
			{
				foreach (var item in EnumerateCachedVideoItem(NicoVideoQuality.Low))
				{
					await item.NicoVideo.DeleteCache(NicoVideoQuality.Low);
				}

				foreach (var item in EnumerateCachedVideoItem(NicoVideoQuality.Original))
				{
					await item.NicoVideo.DeleteCache(NicoVideoQuality.Original);
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
			IsSelectionModeEnable.Value = false;

			if (IncrementalLoadingItems != null)
			{
				IncrementalLoadingItems.BeginLoading -= BeginLoadingItems;
				IncrementalLoadingItems.CompleteLoading -= CompleteLoadingItems;
			}

			try
			{
				var source = GenerateIncrementalSource();

				IncrementalLoadingItems = new IncrementalLoadingCollection<IIncrementalSource<VIDEO_INFO_VM>, VIDEO_INFO_VM>(source, IncrementalLoadCount);

				IncrementalLoadingItems.BeginLoading += BeginLoadingItems;
				IncrementalLoadingItems.CompleteLoading += CompleteLoadingItems;

				PostResetList();
			}
			catch
			{
				IncrementalLoadingItems = null;

				Debug.WriteLine("failed GenerateIncrementalSource.");
			}
		}


		private void BeginLoadingItems()
		{
			NowLoadingItems.Value = true;
		}

		private void CompleteLoadingItems()
		{
			NowLoadingItems.Value = false;
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

		#region Selection


		public ReactiveProperty<bool> IsSelectionModeEnable { get; private set; }
		public ReactiveProperty<ListViewSelectionMode> SelectionMode { get; private set; }


		public ReactiveCommand<VideoInfoControlViewModel> PlayCommand { get; private set; }

		

		private DelegateCommand _EnableSelectionCommand;
		public DelegateCommand EnableSelectionCommand
		{
			get
			{
				return _EnableSelectionCommand
					?? (_EnableSelectionCommand = new DelegateCommand(() => 
					{
						IsSelectionModeEnable.Value = true;
					}));
			}
		}


		private DelegateCommand _DisableSelectionCommand;
		public DelegateCommand DisableSelectionCommand
		{
			get
			{
				return _DisableSelectionCommand
					?? (_DisableSelectionCommand = new DelegateCommand(() =>
					{
						IsSelectionModeEnable.Value = false;
					}));
			}
		}

		#endregion



		public ObservableCollection<VIDEO_INFO_VM> SelectedVideoInfoItems { get; private set; }

		public IncrementalLoadingCollection<IIncrementalSource<VIDEO_INFO_VM>, VIDEO_INFO_VM> IncrementalLoadingItems { get; private set; }

		public ReactiveProperty<double> ListViewVerticalOffset { get; private set; }
		private double _LastListViewOffset;

		public ReactiveProperty<bool> NowLoadingItems { get; private set; }


		public ReactiveCommand PlayAllCommand { get; private set; }
		public ReactiveCommand CancelCacheDownloadRequest { get; private set; }
		public ReactiveCommand RequestOriginalQualityCacheDownload { get; private set; }
		public ReactiveCommand RequestLowQualityCacheDownload { get; private set; }
		public ReactiveCommand DeleteOriginalQualityCache { get; private set; }
		public ReactiveCommand DeleteLowQualityCache { get; private set; }

		// クオリティ指定なしのコマンド
		// VMがクオリティを実装している場合には、そのクオリティを仕様
		// そうでない場合は、リクエスト時は低クオリティのみを
		// 削除時はすべてのクオリティの動画を指定してアクションを実行します。
		// 基本的にキャッシュ管理画面でしか使わないはずです
		public ReactiveCommand RequestCacheDownload { get; private set; }
		public ReactiveCommand DeleteCache { get; private set; }

		public HohoemaApp HohoemaApp { get; private set; }


	}
}
