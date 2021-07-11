using Hohoema.Models.Helpers;
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
using System.Threading;
using Microsoft.Toolkit.Uwp.UI;
using Windows.UI.Xaml.Data;
using Prism.Navigation;
using System.Reactive.Concurrency;
using Prism.Ioc;
using Windows.System;
using Uno.Threading;
using Hohoema.Models.UseCase;
using Microsoft.Toolkit.Collections;
using Microsoft.Toolkit.Uwp;
using Uno.Disposables;

namespace Hohoema.Presentation.ViewModels
{


    public abstract class HohoemaListingPageViewModelBase<ITEM_VM> : HohoemaPageViewModelBase
    {

        /// <summary>
        /// ListViewBaseが初回表示時に複数回取得動作を実行してしまう問題を回避するためのワークアラウンド
        /// １回目の取得内容を２分割して２回に分けて届ける。３回目以降はそのまま取得して返すだけ。
        /// </summary>
        /// <remarks>ListViewBase.IncrementalLoadingThreshold が未指定でないと動作しない</remarks>
        public class HohoemaIncrementalLoadingCollection : IncrementalLoadingCollection<IIncrementalSource<ITEM_VM>, ITEM_VM>
        {
            public HohoemaIncrementalLoadingCollection(int itemsPerPage = 20, Action onStartLoading = null, Action onEndLoading = null, Action<Exception> onError = null) : base(itemsPerPage, onStartLoading, onEndLoading, onError)
            {
            }

            public HohoemaIncrementalLoadingCollection(IIncrementalSource<ITEM_VM> source, int itemsPerPage = 20, Action onStartLoading = null, Action onEndLoading = null, Action<Exception> onError = null) : base(source, itemsPerPage, onStartLoading, onEndLoading, onError)
            {
            }

            
            int _timing = 0;
            List<ITEM_VM> _dividedPresentItemsSource;
            int _firstItemsCount = 0;

            protected override async Task<IEnumerable<ITEM_VM>> LoadDataAsync(CancellationToken cancellationToken)
            {
                _timing++;
                if (_timing == 1)
                {
                    var itemsEnumerable = await base.LoadDataAsync(cancellationToken);
                    var listedItems = itemsEnumerable.ToList();
                    if (!listedItems.Any())
                    {
                        return Enumerable.Empty<ITEM_VM>();
                    }

                    if (listedItems.Count == 1)
                    {
                        return listedItems;
                    }

                    _dividedPresentItemsSource = listedItems;

                    var firstItems = _dividedPresentItemsSource.Take(_dividedPresentItemsSource.Count / 2);
                    _firstItemsCount = firstItems.Count();
                    return firstItems;
                }
                else if (_timing == 2)
                {
                    if (_dividedPresentItemsSource == null)
                    {
                        return Enumerable.Empty<ITEM_VM>();
                    }

                    var items = _dividedPresentItemsSource;
                    _dividedPresentItemsSource = null;
                    return items.Skip(_firstItemsCount);
                }
                else
                {
                    return await base.LoadDataAsync(cancellationToken);
                }
            }
        }




        public ReactiveProperty<int> MaxItemsCount { get; private set; }
        public ReactiveProperty<int> LoadedItemsCount { get; private set; }

        public AdvancedCollectionView ItemsView { get; private set; }
        private static AdvancedCollectionView _cachedItemsView;

        public ReactiveProperty<bool> NowLoading { get; private set; }
        public ReactiveProperty<bool> CanChangeSort { get; private set; }

        public ReactiveProperty<bool> NowRefreshable { get; private set; }

        public ReactiveProperty<bool> HasItem { get; private set; }

        public ReactiveProperty<bool> HasError { get; private set; }

        DispatcherQueue _dispatcherQueue;

        private FastAsyncLock _ItemsUpdateLock = new FastAsyncLock();

        public DateTime LatestUpdateTime = DateTime.Now;

        public HohoemaListingPageViewModelBase()
        {
            NowLoading = new ReactiveProperty<bool>(true)
                .AddTo(_CompositeDisposable);

            HasItem = new ReactiveProperty<bool>(true)
                .AddTo(_CompositeDisposable);

            HasError = new ReactiveProperty<bool>(false)
                .AddTo(_CompositeDisposable);

            MaxItemsCount = new ReactiveProperty<int>(0)
                .AddTo(_CompositeDisposable);

            LoadedItemsCount = new ReactiveProperty<int>(0)
                .AddTo(_CompositeDisposable);

            NowRefreshable = new ReactiveProperty<bool>(false);

            // 読み込み中または選択中はソートを変更できない
            CanChangeSort = Observable.CombineLatest(
                NowLoading
                )
                .Select(x => !x.Any(y => y))
                .ToReactiveProperty()
                .AddTo(_CompositeDisposable);

            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        }


        public override void Dispose()
        {
            if (ItemsView?.Source is IncrementalLoadingCollection<IIncrementalSource<ITEM_VM>, ITEM_VM> oldItems)
            {
                ItemsView.Source = new List<ITEM_VM>();
                ItemsView.Clear();
                ItemsView = null;
                RaisePropertyChanged(nameof(ItemsView));
            }

            base.Dispose();
        }


        public override void OnNavigatingTo(INavigationParameters parameters)
        {
            var navigationMode = parameters.GetNavigationMode();
            if (_cachedItemsView != null && !CheckNeedUpdateOnNavigateTo(navigationMode))
            {
                ItemsView = _cachedItemsView;
                RaisePropertyChanged(nameof(ItemsView));
            }
            else
            {
                DisposeItemsView(_cachedItemsView);
                _cachedItemsView = null;
            }

            base.OnNavigatingTo(parameters);
        }

        public override void OnNavigatedTo(INavigationParameters parameters)
        {
            if (ItemsView == null)
            {
                ResetList();
            }

            base.OnNavigatedTo(parameters);
        }

        public virtual Task OnNavigatedToAsync(INavigationParameters parameters)
        {
            return Task.CompletedTask;
        }

        public override async void OnNavigatedFrom(INavigationParameters parameters)
        {
            using (var releaser = await _ItemsUpdateLock.LockAsync(NavigationCancellationToken))
            {
                _cachedItemsView = ItemsView;

                // Note: ListViewのItemTemplae内でUserControlを利用した場合のメモリリークバグを回避するListView.ItemsSourceにnullを与える
                ItemsView = null;
                RaisePropertyChanged(nameof(ItemsView));
            }

            base.OnNavigatedFrom(parameters);
        }

        private void DisposeItemsView(AdvancedCollectionView acv)
        {
            if (acv == null) { return; }

            foreach (var item in acv)
            {
                item.TryDispose();
            }

            acv?.Source?.TryDispose();
            acv.TryDispose();
        }

        private async Task ResetList_Internal(CancellationToken ct)
        {
            using (var releaser = await _ItemsUpdateLock.LockAsync(ct))
            {
                var prevItemsView = ItemsView;
                ItemsView = null;
                RaisePropertyChanged(nameof(ItemsView));

                NowLoading.Value = true;
                HasItem.Value = true;
                LoadedItemsCount.Value = 0;

                DisposeItemsView(prevItemsView);

                try
                {
                    var (pageSize, source) = GenerateIncrementalSource();

                    if (source == null)
                    {
                        HasItem.Value = false;
                        return;
                    }

                    var items = new HohoemaIncrementalLoadingCollection(source, pageSize, BeginLoadingItems, onEndLoading: CompleteLoadingItems, OnLodingItemError);

                    ItemsView = new AdvancedCollectionView(items);
                    RaisePropertyChanged(nameof(ItemsView));

                    PostResetList();
                }
                catch
                {
                    NowLoading.Value = false;
                    HasError.Value = true;
                    Debug.WriteLine("failed GenerateIncrementalSource.");
                    throw;
                }
            }
        }

        private void OnLodingItemError(Exception e)
        {
            ErrorTrackingManager.TrackError(e);
        }

        protected void ResetList()
        {
            _dispatcherQueue.TryEnqueue(async () => 
            {
                try
                {
                    await ResetList_Internal(NavigationCancellationToken);
                }
                catch (Exception e)
                {
                    ErrorTrackingManager.TrackError(e);
                }
            });
        }


		private void BeginLoadingItems()
		{
			HasError.Value = false;
			NowLoading.Value = true;
		}

		private void CompleteLoadingItems()
		{
			NowLoading.Value = false;

			LoadedItemsCount.Value = ItemsView?.Count ?? 0;
			HasItem.Value = LoadedItemsCount.Value > 0;
        }

		protected virtual void PostResetList()
        {
            LatestUpdateTime = DateTime.Now;
        }

		protected abstract (int PageSize, IIncrementalSource<ITEM_VM> IncrementalSource) GenerateIncrementalSource();

		protected virtual bool CheckNeedUpdateOnNavigateTo(NavigationMode mode)
        {
            if (mode == NavigationMode.New || mode == NavigationMode.Refresh)
            {
                return true;
            }
            else
            {
                return false;
            }

            /*
            

            var elpasedTime = DateTime.Now - LatestUpdateTime;
            if (elpasedTime > TimeSpan.FromMinutes(30))
            {
                return true;
            }

            return false;
            */
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

        
        private DelegateCommand _RefreshCommand;
		public DelegateCommand RefreshCommand
		{
			get
			{
				return _RefreshCommand
					?? (_RefreshCommand = new DelegateCommand(() => 
					{
                        ResetList();
                    }));
			}
		}
	}
}
