using NicoPlayerHohoema.Models;
using NicoPlayerHohoema.Models.Helpers;
using Prism.Commands;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Prism.Windows.Navigation;
using Windows.UI.Xaml.Navigation;
using System.Threading;
using Microsoft.Toolkit.Uwp.UI;
using Windows.UI.Xaml.Data;

namespace NicoPlayerHohoema.ViewModels
{


    public abstract class HohoemaListingPageViewModelBase<ITEM_VM> : HohoemaViewModelBase
	{
        public HohoemaListingPageViewModelBase(
            Services.PageManager pageManager,
            bool useDefaultPageTitle = true
            )
            : base(pageManager)
        {
            NowLoading = new ReactiveProperty<bool>(true)
                .AddTo(_CompositeDisposable);

            SelectedItems = new ObservableCollection<ITEM_VM>();


            HasItem = new ReactiveProperty<bool>(true);

            HasError = new ReactiveProperty<bool>(false);

            MaxItemsCount = new ReactiveProperty<int>(0)
                .AddTo(_CompositeDisposable);
            LoadedItemsCount = new ReactiveProperty<int>(0)
                .AddTo(_CompositeDisposable);
            SelectedItemsCount = SelectedItems.ObserveProperty(x => x.Count)
                .ToReactiveProperty(0)
                .AddTo(_CompositeDisposable);

            IsItemSelected = SelectedItems.ObserveProperty(x => x.Count)
                .Select(x => x > 0)
                .ToReactiveProperty()
                .AddTo(_CompositeDisposable);


            NowRefreshable = new ReactiveProperty<bool>(false);

            // 複数選択モード
            IsSelectionModeEnable = new ReactiveProperty<bool>(false)
                .AddTo(_CompositeDisposable);

            IsSelectionModeEnable.Where(x => !x)
                .Subscribe(x => ClearSelection())
                .AddTo(_CompositeDisposable);

            // 複数選択モードによって再生コマンドの呼び出しを制御する
            SelectItemCommand = IsSelectionModeEnable
                .Select(x => !x)
                .ToReactiveCommand<object>()
                .AddTo(_CompositeDisposable);

            ScrollPosition = new ReactiveProperty<double>();


            //			var SelectionItemsChanged = SelectedItems.ToCollectionChanged().ToUnit();
            /*
                        SelectionItemsChanged.Subscribe(_ => 
                        {
                            if (!IsSelectionModeEnable.Value)
                            {
                                var item = SelectedItems.FirstOrDefault();
                                if (item != null)
                                {
                                    if (item.PrimaryCommand.CanExecute(null))
                                    {
                                        item.PrimaryCommand.Execute(null);
                                    }
                                }
                            }
                        });
                        */
#if DEBUG
            SelectedItems.CollectionChangedAsObservable()
                .Subscribe(x =>
                {
                    Debug.WriteLine("Selected Count: " + SelectedItems.Count);
                })
                .AddTo(_CompositeDisposable);
#endif

            // 読み込み厨または選択中はソートを変更できない
            CanChangeSort = Observable.CombineLatest(
                NowLoading,
                IsSelectionModeEnable
                )
                .Select(x => !x.Any(y => y))
                .ToReactiveProperty()
                .AddTo(_CompositeDisposable);


        }


        public class HohoemaListingCache
        {
            public ISupportIncrementalLoading List;
            public double ScrollPosition;
        }

        static Dictionary<int, HohoemaListingCache> _ListingCache = new Dictionary<int, HohoemaListingCache>();

        protected static int MaxListingCache = 5;

        protected static bool TryGetListingCache(string navigationId, out HohoemaListingCache outCache)
        {
            if (_ListingCache.TryGetValue(navigationId.GetHashCode(), out var cached))
            {
                outCache = cached;
            }
            else outCache = null;

            return outCache != null;
        }

        protected static void AddOrUpdateListingCache(string navigationId, ISupportIncrementalLoading list, double scrollPosition)
        {
            var hash = navigationId.GetHashCode();
            if (_ListingCache.TryGetValue(hash, out var cached))
            {
                // 登録済みの場合は一旦削除して再登録することで辞書位置を更新する
                _ListingCache.Remove(hash);
                cached.ScrollPosition = scrollPosition;
                _ListingCache.Add(hash, cached);
            }
            else
            {
                // キャッシュ上限以上の場合は古いアイテムを削除
                if (_ListingCache.Count > MaxListingCache)
                {
                    // Dictionary の Last() は辞書へより先に追加されたアイテムが取得できる
                    // https://stackoverflow.com/questions/436954/whos-on-dictionary-first
                    _ListingCache.Remove(_ListingCache.Last().Key);
                }

                _ListingCache.Add(hash, new HohoemaListingCache() { List = list, ScrollPosition = scrollPosition });
            }
        }

        string _NavigationId;
        private string NavigationId
        {
            get { return _NavigationId; }
        }


        private AsyncLock _ItemsUpdateLock = new AsyncLock();

		
        public DateTime LatestUpdateTime = DateTime.Now;

        protected override void OnDispose()
		{
            if (IncrementalLoadingItems != null)
            {
                IncrementalLoadingItems.BeginLoading -= BeginLoadingItems;
                IncrementalLoadingItems.DoneLoading -= CompleteLoadingItems;
                if (IncrementalLoadingItems.Source is HohoemaIncrementalSourceBase<ITEM_VM>)
                {
                    (IncrementalLoadingItems.Source as HohoemaIncrementalSourceBase<ITEM_VM>).Error -= HohoemaIncrementalSource_Error;
                }
                IncrementalLoadingItems.Dispose();
                IncrementalLoadingItems = null;
            }
        }
		public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
            HasItem.Value = true;

            _NavigationId = ResolveNavigationId(e);

            if (CheckNeedUpdateOnNavigateTo(e.NavigationMode))
			{
//				IncrementalLoadingItems = null;
			}
			else
			{
				ChangeCanIncmentalLoading(true);
			}

			base.OnNavigatedTo(e, viewModelState);
		}


		protected override async Task NavigatedToAsync(CancellationToken cancelToken, NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			await ListPageNavigatedToAsync(cancelToken, e, viewModelState);

            if (e.NavigationMode == NavigationMode.Back || e.NavigationMode == NavigationMode.Forward)
            {
                if (TryGetListingCache(NavigationId, out var cached))
                {
                    var cachedList = cached.List as IncrementalLoadingCollection<IIncrementalSource<ITEM_VM>, ITEM_VM>;
                    
                    if (!ReferenceEquals(IncrementalLoadingItems, cachedList))
                    {
                        // DataTriggerBehaviorがBackナビゲーション時に反応しない問題の対策
                        await Task.Delay(100);

                        ScrollPosition.Value = cached.ScrollPosition;
                        IncrementalLoadingItems = cachedList;
                        RaisePropertyChanged(nameof(IncrementalLoadingItems));
                        IncrementalLoadingItems.BeginLoading += BeginLoadingItems;
                        IncrementalLoadingItems.DoneLoading += CompleteLoadingItems;

                        if (IncrementalLoadingItems.Source is HohoemaIncrementalSourceBase<ITEM_VM>)
                        {
                            (IncrementalLoadingItems.Source as HohoemaIncrementalSourceBase<ITEM_VM>).Error += HohoemaIncrementalSource_Error;
                        }

                        ItemsView.Source = IncrementalLoadingItems;
                        RaisePropertyChanged(nameof(ItemsView));

                        PostResetList();

                        Debug.WriteLine($"restored {NavigationId} : {ScrollPosition.Value}");
                    }

                    ChangeCanIncmentalLoading(true);
                }
                else
                {
                    ScrollPosition.Value = 0.0;
                    await ResetList();
                }
            }
            else
            {
                if (IncrementalLoadingItems == null
                    || CheckNeedUpdateOnNavigateTo(e.NavigationMode))
                {
                    ScrollPosition.Value = 0.0;
                    await ResetList();
                }
            }
        }

		protected override Task OnResumed()
		{
            ChangeCanIncmentalLoading(true);

            return base.OnResumed();
		}

		protected virtual Task ListPageNavigatedToAsync(CancellationToken cancelToken, NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			return Task.CompletedTask;
		}


		protected override void OnHohoemaNavigatingFrom(NavigatingFromEventArgs e, Dictionary<string, object> viewModelState, bool suspending)
		{
			base.OnHohoemaNavigatingFrom(e, viewModelState, suspending);

            if (!suspending)
			{
				ChangeCanIncmentalLoading(false);

                if (IncrementalLoadingItems != null)
                {
                    AddOrUpdateListingCache(NavigationId, IncrementalLoadingItems, ScrollPosition.Value);

                    Debug.WriteLine($"saved {NavigationId} : {ScrollPosition.Value}");
                }
            }
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
            using (var releaser = await _ItemsUpdateLock.LockAsync())
            {
                HasItem.Value = true;
                LoadedItemsCount.Value = 0;

                IsSelectionModeEnable.Value = false;

                SelectedItems.Clear();

                if (IncrementalLoadingItems != null)
                {
                    if (IncrementalLoadingItems.Source is HohoemaIncrementalSourceBase<ITEM_VM>)
                    {
                        (IncrementalLoadingItems.Source as HohoemaIncrementalSourceBase<ITEM_VM>).Error -= HohoemaIncrementalSource_Error;
                    }
                    IncrementalLoadingItems.BeginLoading -= BeginLoadingItems;
                    IncrementalLoadingItems.DoneLoading -= CompleteLoadingItems;
//                    IncrementalLoadingItems.Dispose();
                    IncrementalLoadingItems = null;
                    RaisePropertyChanged(nameof(IncrementalLoadingItems));
                }

                try
                {
                    var source = GenerateIncrementalSource();

                    if (source == null)
                    {
                        HasItem.Value = false;
                        return;
                    }

                    MaxItemsCount.Value = await source.ResetSource();

                    IncrementalLoadingItems = new IncrementalLoadingCollection<IIncrementalSource<ITEM_VM>, ITEM_VM>(source);
                    RaisePropertyChanged(nameof(IncrementalLoadingItems));

                    IncrementalLoadingItems.BeginLoading += BeginLoadingItems;
                    IncrementalLoadingItems.DoneLoading += CompleteLoadingItems;

                    if (IncrementalLoadingItems.Source is HohoemaIncrementalSourceBase<ITEM_VM>)
                    {
                        (IncrementalLoadingItems.Source as HohoemaIncrementalSourceBase<ITEM_VM>).Error += HohoemaIncrementalSource_Error;
                    }


                    ItemsView = new AdvancedCollectionView(IncrementalLoadingItems);
                    
                    RaisePropertyChanged(nameof(ItemsView));


                    PostResetList();
                }
                catch
                {
                    IncrementalLoadingItems = null;
                    NowLoading.Value = false;
                    HasError.Value = true;
                    Debug.WriteLine("failed GenerateIncrementalSource.");
                }
            }
		}

        protected virtual string ResolveNavigationId(NavigatedToEventArgs e)
        {
            if (e.SourcePageType == null)
            {
                return "empty";
            }

            if (e.Parameter is string strParam)
            {
                return $"{e.SourcePageType.Name}_{strParam}";
            }
            else
            {
                return e.SourcePageType.Name;
            }
        }

		private void HohoemaIncrementalSource_Error()
		{
			HasError.Value = true;
		}

		private void BeginLoadingItems()
		{
			HasError.Value = false;
			NowLoading.Value = true;
		}

		private void CompleteLoadingItems()
		{
			NowLoading.Value = false;

			LoadedItemsCount.Value = IncrementalLoadingItems?.Count ?? 0;
			HasItem.Value = LoadedItemsCount.Value > 0;
        }

		protected virtual void PostResetList()
        {
            LatestUpdateTime = DateTime.Now;
        }

		protected abstract IIncrementalSource<ITEM_VM> GenerateIncrementalSource();

		protected virtual bool CheckNeedUpdateOnNavigateTo(NavigationMode mode)
        {
            if (mode == NavigationMode.New)
            {
                return true;
            }

            var elpasedTime = DateTime.Now - LatestUpdateTime;
            if (elpasedTime > TimeSpan.FromMinutes(30))
            {
                return true;
            }

            return false;
        }

		protected void ClearSelection()
		{
			SelectedItems.Clear();
		}

        private DelegateCommand _ResetSortCommand;
        public DelegateCommand ResetSortCommand
        {
            get
            {
                return _ResetSortCommand
                    ?? (_ResetSortCommand = new DelegateCommand(() =>
                    {
                        ResetSort();
                    }
                    ));
            }
        }

        private DelegateCommand<string> _SortAscendingCommand;
        public DelegateCommand<string> SortAscendingCommand
        {
            get
            {
                return _SortAscendingCommand
                    ?? (_SortAscendingCommand = new DelegateCommand<string>(propertyName => 
                    {
                        AddSortDescription(new SortDescription(propertyName, SortDirection.Ascending), withReset:true);
                    }
                    ));
            }
        }

        private DelegateCommand<string> _SortDescendingCommand;
        public DelegateCommand<string> SortDescendingCommand
        {
            get
            {
                return _SortDescendingCommand
                    ?? (_SortDescendingCommand = new DelegateCommand<string>(propertyName =>
                    {
                        AddSortDescription(new SortDescription(propertyName, SortDirection.Descending), withReset: true);
                    }
                    ));
            }
        }



        protected void AddSortDescription(SortDescription sort, bool withReset = false)
        {
            if (withReset)
            {
                ItemsView.SortDescriptions.Clear();
            }

            ItemsView.SortDescriptions.Add(sort);
            ItemsView.RefreshSorting();
        }

        protected void ResetSort()
        {
            ItemsView.SortDescriptions.Clear();
            ItemsView.RefreshSorting();
        }


        protected void AddFilter(Predicate<ITEM_VM> predicate)
        {
            ItemsView.Filter = (p) => predicate((ITEM_VM)p);
            ItemsView.RefreshFilter();
        }

        protected void ResetFilter()
        {
            ItemsView.Filter = null;
            ItemsView.RefreshFilter();
        }

		#region Selection


		public ReactiveProperty<bool> IsSelectionModeEnable { get; private set; }
		

		public ReactiveCommand<object> SelectItemCommand { get; private set; }

                
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
					?? (_RefreshCommand = new DelegateCommand(async () => 
					{
						await ResetList();
					}));
			}
		}

		public ReactiveProperty<int> MaxItemsCount { get; private set; }
		public ReactiveProperty<int> LoadedItemsCount { get; private set; }
		public ReactiveProperty<int> SelectedItemsCount { get; private set; }

        public ReactiveProperty<bool> IsItemSelected { get; private set; }

        public ObservableCollection<ITEM_VM> SelectedItems { get; private set; }

		public IncrementalLoadingCollection<IIncrementalSource<ITEM_VM>, ITEM_VM> IncrementalLoadingItems { get; private set; }

        public AdvancedCollectionView ItemsView { get; private set; } = new AdvancedCollectionView();

        public ReactiveProperty<double> ScrollPosition { get; }

        public ReactiveProperty<bool> NowLoading { get; private set; }
        public ReactiveProperty<bool> CanChangeSort { get; private set; }

        public ReactiveProperty<bool> NowRefreshable { get; private set; }

		public ReactiveProperty<bool> HasItem { get; private set; }

		public ReactiveProperty<bool> HasError { get; private set; }


	}
}
