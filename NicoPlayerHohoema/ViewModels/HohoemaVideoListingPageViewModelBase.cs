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
using Windows.UI.Xaml.Navigation;
using System.Threading;
using Windows.UI.Xaml;

namespace NicoPlayerHohoema.ViewModels
{
	abstract public class HohoemaVideoListingPageViewModelBase<VIDEO_INFO_VM> : HohoemaViewModelBase
		where VIDEO_INFO_VM : VideoInfoControlViewModel
	{
		public HohoemaVideoListingPageViewModelBase(HohoemaApp app, PageManager pageManager, bool isRequireSignIn = false)
			: base(app, pageManager, isRequireSignIn)
		{
			NowLoadingItems = new ReactiveProperty<bool>(true)
				.AddTo(_CompositeDisposable);
			SelectedVideoInfoItems = new ObservableCollection<VIDEO_INFO_VM>();

			HasVideoInfoItem = new ReactiveProperty<bool>(true);

			ListViewVerticalOffset = new ReactiveProperty<double>(0.0)
				.AddTo(_CompositeDisposable);
			_LastListViewOffset = 0;


			NowRefreshable = new ReactiveProperty<bool>(false);

			// 複数選択モード
			IsSelectionModeEnable = new ReactiveProperty<bool>(false)
				.AddTo(_CompositeDisposable);

			IsSelectionModeEnable.Where(x => !x)
				.Subscribe(x => ClearSelection())
				.AddTo(_CompositeDisposable);

			// 複数選択モードによって再生コマンドの呼び出しを制御する
			PlayCommand = IsSelectionModeEnable
				.Select(x => !x)
				.ToReactiveCommand<VideoInfoControlViewModel>()
				.AddTo(_CompositeDisposable);


			PlayCommand.Subscribe(x =>
			{
				if (x?.PlayCommand.CanExecute() ?? false)
				{
					x?.PlayCommand.Execute();
				}

				ClearSelection();
			})
			.AddTo(_CompositeDisposable);


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
				.ToReactiveCommand(false)
				.AddTo(_CompositeDisposable);

			PlayAllCommand
				.SubscribeOnUIDispatcher()
				.Subscribe(_ => 
			{

				// TODO: プレイリストに登録
				// プレイリストを空にしてから選択動画を登録

				SelectedVideoInfoItems.First()?.PlayCommand.Execute();
			})
			.AddTo(_CompositeDisposable);

			CancelCacheDownloadRequest = SelectionItemsChanged
				.Select(_ => EnumerateDownloadingVideoItems().Count() > 0)
				.ToReactiveCommand(false)
				.AddTo(_CompositeDisposable);

			CancelCacheDownloadRequest
				.SubscribeOnUIDispatcher()
				.Subscribe(_ => 
			{
				foreach (var item in EnumerateDownloadingVideoItems())
				{
					item.NicoVideo.CancelCacheRequest();
				}

				ClearSelection();
				UpdateList();
			})
			.AddTo(_CompositeDisposable);

			RequestOriginalQualityCacheDownload = SelectionItemsChanged
				.Select(_ =>
				{
					return EnumerateCanDownloadVideoItem(NicoVideoQuality.Original).Count() > 0
						&& HohoemaApp.UserSettings.CacheSettings.IsUserAcceptedCache;
				}			 
				)
				.ToReactiveCommand(false)
				.AddTo(_CompositeDisposable);

			RequestOriginalQualityCacheDownload
				.SubscribeOnUIDispatcher()
				.Subscribe(async _ => 
			{
				foreach (var item in EnumerateCanDownloadVideoItem(NicoVideoQuality.Original))
				{
					await item.NicoVideo.RequestCache(NicoVideoQuality.Original);
				}

				ClearSelection();
				UpdateList();
			})
			.AddTo(_CompositeDisposable);

			RequestLowQualityCacheDownload = SelectionItemsChanged
				.Select(_ =>
				{
					return EnumerateCanDownloadVideoItem(NicoVideoQuality.Low).Count() > 0
						&& HohoemaApp.UserSettings.CacheSettings.IsUserAcceptedCache;
				})
				.ToReactiveCommand(false)
				.AddTo(_CompositeDisposable);

			RequestLowQualityCacheDownload
				.SubscribeOnUIDispatcher()
				.Subscribe(async _ =>
			{
				foreach (var item in EnumerateCanDownloadVideoItem(NicoVideoQuality.Low))
				{
					await item.NicoVideo.RequestCache(NicoVideoQuality.Low);
				}

				ClearSelection();
				UpdateList();

			})
			.AddTo(_CompositeDisposable);

			DeleteOriginalQualityCache = SelectionItemsChanged
				.Select(_ => EnumerateCachedVideoItem(NicoVideoQuality.Original).Count() > 0)
				.ToReactiveCommand(false)
				.AddTo(_CompositeDisposable);

			DeleteOriginalQualityCache
				.SubscribeOnUIDispatcher()
				.Subscribe(async _ =>
			{
				foreach (var item in EnumerateCachedVideoItem(NicoVideoQuality.Original))
				{
					await item.NicoVideo.CancelCacheRequest(NicoVideoQuality.Original);
				}

				ClearSelection();
				ResetList();
			})
			.AddTo(_CompositeDisposable);

			DeleteLowQualityCache = SelectionItemsChanged
				.Select(_ => EnumerateCachedVideoItem(NicoVideoQuality.Low).Count() > 0)
				.ToReactiveCommand(false)
				.AddTo(_CompositeDisposable);
			DeleteLowQualityCache
				.SubscribeOnUIDispatcher()
				.Subscribe(async _ =>
			{
				foreach (var item in EnumerateCachedVideoItem(NicoVideoQuality.Low))
				{
					await item.NicoVideo.CancelCacheRequest(NicoVideoQuality.Low);
				}

				ClearSelection();
				ResetList();
			})
			.AddTo(_CompositeDisposable);

			// クオリティ指定無しのキャッシュDLリクエスト
			RequestCacheDownload = SelectionItemsChanged
				.Select(_ => EnumerateCanDownloadVideoItem(NicoVideoQuality.Low).Count() > 0 && HohoemaApp.UserSettings.CacheSettings.IsUserAcceptedCache)
				.ToReactiveCommand(false)
				.AddTo(_CompositeDisposable);

			RequestCacheDownload
				.SubscribeOnUIDispatcher()
				.Subscribe(async _ =>
			{
				foreach (var item in EnumerateCanDownloadVideoItem(NicoVideoQuality.Low))
				{
					await item.NicoVideo.RequestCache(NicoVideoQuality.Low);
				}

				ClearSelection();
				UpdateList();
			})
			.AddTo(_CompositeDisposable);

			var dispacther = Window.Current.CoreWindow.Dispatcher;
			// クオリティ指定無しのキャッシュ削除
			DeleteCache = SelectionItemsChanged
				.Select(_ => EnumerateCachedVideoItem(NicoVideoQuality.Low).Count() > 0 || EnumerateCachedVideoItem(NicoVideoQuality.Original).Count() > 0)
				.ToReactiveCommand(false)
				.AddTo(_CompositeDisposable);
			DeleteCache
				.SubscribeOnUIDispatcher()
				.Subscribe(async _ =>
			{
				foreach (var item in EnumerateCachedVideoItem(NicoVideoQuality.Low))
				{
					await item.NicoVideo.CancelCacheRequest(NicoVideoQuality.Low);
				}

				foreach (var item in EnumerateCachedVideoItem(NicoVideoQuality.Original))
				{
					await item.NicoVideo.CancelCacheRequest(NicoVideoQuality.Original);
				}
				
				ClearSelection();
				ResetList();
			})
			.AddTo(_CompositeDisposable);



		}


		protected override void OnDispose()
		{
			IncrementalLoadingItems?.Dispose();
		}


		protected override void OnSignIn(ICollection<IDisposable> disposer)
		{
			base.OnSignIn(disposer);

			HohoemaApp.UserSettings.CacheSettings.ObserveProperty(x => x.IsUserAcceptedCache)
				.Subscribe(x => CanDownload = x)
				.AddTo(disposer);

			
		}

		protected override void OnSignOut()
		{
			base.OnSignOut();

			if (IsRequireSignIn)
			{
				IncrementalLoadingItems = null;
			}
		}

		public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			base.OnNavigatedTo(e, viewModelState);

			if (IncrementalLoadingItems == null
				|| CheckNeedUpdateOnNavigateTo(e.NavigationMode))
			{
//				ResetList();
			}
			
		}


		protected override async Task NavigatedToAsync(CancellationToken cancelToken, NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			if (!NowSignIn && PageIsRequireSignIn)
			{
				IncrementalLoadingItems = null;
				return;
			}

			await ListPageNavigatedToAsync(cancelToken, e, viewModelState);

			if (_IncrementalLoadingItems != null)
			{
				IncrementalLoadingItems = _IncrementalLoadingItems;
				_IncrementalLoadingItems = null;
			}

			if (IncrementalLoadingItems == null
				|| CheckNeedUpdateOnNavigateTo(e.NavigationMode))
			{
				ResetList();
			}
			else
			{
				OnPropertyChanged(nameof(IncrementalLoadingItems));

				await Task.Delay(100);

				ListViewVerticalOffset.Value = _LastListViewOffset;
				ChangeCanIncmentalLoading(true);				
			}
		}

		protected virtual Task ListPageNavigatedToAsync(CancellationToken cancelToken, NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			return Task.CompletedTask;
		}

		public override void OnNavigatingFrom(NavigatingFromEventArgs e, Dictionary<string, object> viewModelState, bool suspending)
		{
			base.OnNavigatingFrom(e, viewModelState, suspending);

			_LastListViewOffset = ListViewVerticalOffset.Value;
			ListViewVerticalOffset.Value = 0.0;
			ChangeCanIncmentalLoading(false);

			_IncrementalLoadingItems = IncrementalLoadingItems;
			IncrementalLoadingItems = null;
		}


		private void ChangeCanIncmentalLoading(bool enableLoading)
		{
			if (IncrementalLoadingItems != null)
			{
				IncrementalLoadingItems.IsPuaseLoading = !enableLoading;
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
				IncrementalLoadingItems.DoneLoading -= CompleteLoadingItems;
				IncrementalLoadingItems.Dispose();
				IncrementalLoadingItems = null;
			}

			try
			{
				var source = GenerateIncrementalSource();

				IncrementalLoadingItems = new IncrementalLoadingCollection<IIncrementalSource<VIDEO_INFO_VM>, VIDEO_INFO_VM>(source, IncrementalLoadCount);
				OnPropertyChanged(nameof(IncrementalLoadingItems));

				IncrementalLoadingItems.BeginLoading += BeginLoadingItems;
				IncrementalLoadingItems.DoneLoading += CompleteLoadingItems;

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

			HasVideoInfoItem.Value = IncrementalLoadingItems?.Count > 0;
		}

		protected virtual void PostResetList() { }

		protected abstract uint IncrementalLoadCount { get; }

		abstract protected IIncrementalSource<VIDEO_INFO_VM> GenerateIncrementalSource();

		protected virtual bool CheckNeedUpdateOnNavigateTo(NavigationMode mode) { return mode != NavigationMode.Back; }

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
						.Where(x => x.NicoVideo.OriginalQualityCacheState != null);
				case NicoVideoQuality.Low:
					return qualityFilterdVideoItems
						.Where(x => x.NicoVideo.LowQualityCacheState != null);
				default:
					return Enumerable.Empty<VideoInfoControlViewModel>();
			}
		}



		protected void ClearSelection()
		{
			SelectedVideoInfoItems.Clear();
		}

		#region Selection


		public ReactiveProperty<bool> IsSelectionModeEnable { get; private set; }
		

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



		private DelegateCommand _RefreshCommand;
		public DelegateCommand RefreshCommand
		{
			get
			{
				return _RefreshCommand
					?? (_RefreshCommand = new DelegateCommand(() => 
					{
						IncrementalLoadingItems.Clear();
					}));
			}
		}

		private bool _CanDownload;
		public bool CanDownload
		{
			get { return _CanDownload; }
			set { SetProperty(ref _CanDownload, value); }
		}



		public ObservableCollection<VIDEO_INFO_VM> SelectedVideoInfoItems { get; private set; }

		public IncrementalLoadingCollection<IIncrementalSource<VIDEO_INFO_VM>, VIDEO_INFO_VM> IncrementalLoadingItems { get; private set; }
		private IncrementalLoadingCollection<IIncrementalSource<VIDEO_INFO_VM>, VIDEO_INFO_VM> _IncrementalLoadingItems;

		public ReactiveProperty<double> ListViewVerticalOffset { get; private set; }
		private double _LastListViewOffset;

		public ReactiveProperty<bool> NowLoadingItems { get; private set; }

		public ReactiveProperty<bool> NowRefreshable { get; private set; }

		public ReactiveProperty<bool> HasVideoInfoItem { get; private set; }

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

		public bool PageIsRequireSignIn { get; private set; }
		public bool NowSignedIn { get; private set; }

	}
}
