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
using NicoPlayerHohoema.Views.Service;
using Microsoft.Practices.Unity;

namespace NicoPlayerHohoema.ViewModels
{
	abstract public class HohoemaVideoListingPageViewModelBase<VIDEO_INFO_VM> : HohoemaViewModelBase
		where VIDEO_INFO_VM : VideoInfoControlViewModel
	{
		public HohoemaVideoListingPageViewModelBase(HohoemaApp app, PageManager pageManager, MylistRegistrationDialogService mylistDialogService, bool isRequireSignIn = false)
			: base(app, pageManager, isRequireSignIn)
		{
			MylistDialogService = mylistDialogService;

			NowLoadingItems = new ReactiveProperty<bool>(true)
				.AddTo(_CompositeDisposable);
			SelectedVideoInfoItems = new ObservableCollection<VIDEO_INFO_VM>();

			HasVideoInfoItem = new ReactiveProperty<bool>(true);

			ListViewVerticalOffset = new ReactiveProperty<double>(0.0)
				.AddTo(_CompositeDisposable);
			_LastListViewOffset = 0;


			MaxItemsCount = new ReactiveProperty<int>(0)
				.AddTo(_CompositeDisposable);
			LoadedItemsCount = new ReactiveProperty<int>(0)
				.AddTo(_CompositeDisposable);
			SelectedItemsCount = SelectedVideoInfoItems.ObserveProperty(x => x.Count)
				.ToReactiveProperty(0)
				.AddTo(_CompositeDisposable);



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
				.Select(_ => EnumerateCacheRequestedVideoItems().Count() > 0)
				.ToReactiveCommand(false)
				.AddTo(_CompositeDisposable);

			CancelCacheDownloadRequest
				.SubscribeOnUIDispatcher()
				.Subscribe(async _ => 
			{
				foreach (var item in EnumerateCacheRequestedVideoItems())
				{
					if (item is CacheVideoViewModel)
					{
						var quality = (item as CacheVideoViewModel).Quality;
						await item.NicoVideo.CancelCacheRequest(quality);
					}
					else
					{
						await item.NicoVideo.CancelCacheRequest();
					}
				}

				ClearSelection();
//				await UpdateList();
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
				await UpdateList();
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
				await UpdateList();

			})
			.AddTo(_CompositeDisposable);

			

			// クオリティ指定無しのキャッシュDLリクエスト
			RequestCacheDownload = SelectionItemsChanged
				.Select(_ => EnumerateCanDownloadVideoItem().Count() > 0 && HohoemaApp.UserSettings.CacheSettings.IsUserAcceptedCache)
				.ToReactiveCommand(false)
				.AddTo(_CompositeDisposable);

			RequestCacheDownload
				.SubscribeOnUIDispatcher()
				.Subscribe(async _ =>
			{
				// 低画質限定を指定されている場合はそれに従う
				if (HohoemaApp.UserSettings.PlayerSettings.IsLowQualityDeafult)
				{
					foreach (var item in EnumerateCanDownloadVideoItem())
					{
						if (item.NicoVideo.IsOriginalQualityOnly)
						{
							await item.NicoVideo.RequestCache(NicoVideoQuality.Original);
						}
						else
						{
							await item.NicoVideo.RequestCache(NicoVideoQuality.Low);
						}
					}
				}

				// そうでない場合は、オリジナル画質を優先して現在ダウンロード可能な画質でキャッシュリクエスト
				else
				{
					foreach (var item in EnumerateCanDownloadVideoItem(/*画質指定なし*/))
					{
						if (item.NicoVideo.CanRequestDownloadOriginalQuality)
						{
							await item.NicoVideo.RequestCache(NicoVideoQuality.Original);
						}
						else if (item.NicoVideo.CanRequestDownloadLowQuality)
						{
							await item.NicoVideo.RequestCache(NicoVideoQuality.Low);
						}
					}
				}
				
				ClearSelection();
				await UpdateList();
			})
			.AddTo(_CompositeDisposable);

			

			RegistratioMylistCommand = SelectionItemsChanged
				.Select(x => SelectedVideoInfoItems.Count > 0)
				.ToReactiveCommand(false)
				.AddTo(_CompositeDisposable);
			RegistratioMylistCommand
				.SubscribeOnUIDispatcher()
				.Subscribe(async _ => 
				{
					var result = await MylistDialogService.ShowDialog(SelectedVideoInfoItems);

					if (result == null) { return; }

					var mylistGroup = result.Item1;
					var mylistComment = result.Item2;

					Debug.WriteLine($"一括マイリスト登録を開始...");
					int successCount = 0;
					int existCount = 0;
					int failedCount = 0;
					foreach (var video in SelectedVideoInfoItems)
					{
						var registrationResult = await mylistGroup.Registration(
							video.RawVideoId
							, mylistComment
							, withRefresh : false /* あとで一括でリフレッシュ */
							);

						switch (registrationResult)
						{
							case Mntone.Nico2.ContentManageResult.Success: successCount++; break;
							case Mntone.Nico2.ContentManageResult.Exist:   existCount++; break;
							case Mntone.Nico2.ContentManageResult.Failed:  failedCount++; break;
							default:
								break;
						}

						Debug.WriteLine($"{video.Title}[{video.RawVideoId}]:{registrationResult.ToString()}");
					}

					// リフレッシュ
					await mylistGroup.Refresh();


					// ユーザーに結果を通知

					var titleText = $"「{mylistGroup.Name}」に {successCount}件 の動画を登録しました";
					var toastService = App.Current.Container.Resolve<ToastNotificationService>();
					var resultText = $"";
					if (existCount > 0)
					{
						resultText += $"重複：{existCount} 件";
					}
					if (failedCount > 0)
					{
						resultText += $"\n登録に失敗した {failedCount}件 は選択されたままです";
					}

					toastService.ShowText(titleText, resultText);



					// マイリスト登録に失敗したものを残すように
					// 登録済みのアイテムを選択アイテムリストから削除
					foreach (var item in SelectedVideoInfoItems.ToArray())
					{
						if (mylistGroup.CheckRegistratedVideoId(item.RawVideoId))
						{
							SelectedVideoInfoItems.Remove(item);
						}
					}

//					ResetList();

					Debug.WriteLine($"一括マイリスト登録を完了---------------");
				});
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
				await ResetList();
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

		protected async Task UpdateList()
		{
			// TODO: 表示中のアイテムすべての状態を更新
			// 主にキャッシュ状態の更新が目的

			var source = IncrementalLoadingItems?.Source;

			if (source != null)
			{
				MaxItemsCount.Value = await source.ResetSource();
			}
		}

		protected async Task ResetList()
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

				MaxItemsCount.Value = await source.ResetSource();

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

			LoadedItemsCount.Value = IncrementalLoadingItems?.Count ?? 0;
			HasVideoInfoItem.Value = LoadedItemsCount.Value > 0;
		}

		protected virtual void PostResetList() { }

		protected abstract uint IncrementalLoadCount { get; }

		abstract protected IIncrementalSource<VIDEO_INFO_VM> GenerateIncrementalSource();

		protected virtual bool CheckNeedUpdateOnNavigateTo(NavigationMode mode) { return mode != NavigationMode.Back; }

		private IEnumerable<VideoInfoControlViewModel> EnumerateCacheRequestedVideoItems()
		{
			return SelectedVideoInfoItems.Where(x =>
			{
				return x.NicoVideo.OriginalQualityCacheState.HasValue
					|| x.NicoVideo.LowQualityCacheState.HasValue;
					
			});
		}

		private IEnumerable<VideoInfoControlViewModel> EnumerateCanDownloadVideoItem(NicoVideoQuality? quality = null)
		{
			if (!quality.HasValue)
			{
				return SelectedVideoInfoItems.Where(x =>
				{
					var video = x.NicoVideo;
					if (x.NicoVideo.CanRequestDownloadOriginalQuality)
					{
						return true;
					}
					else if (video.CanRequestDownloadLowQuality)
					{
						return true;
					}
					else
					{
						return false;
					}
				});
			}
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


		public ReactiveProperty<int> MaxItemsCount { get; private set; }
		public ReactiveProperty<int> LoadedItemsCount { get; private set; }
		public ReactiveProperty<int> SelectedItemsCount { get; private set; }


		public MylistRegistrationDialogService MylistDialogService { get; private set; }

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

		public ReactiveCommand RegistratioMylistCommand { get; private set; }


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
