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

namespace Hohoema.Presentation.ViewModels
{


    public abstract class HohoemaListingPageViewModelBase<ITEM_VM> : HohoemaPageViewModelBase
    {

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

                if (oldItems.Source is HohoemaIncrementalSourceBase<ITEM_VM> hohoemaIncrementalSource)
                {
                    hohoemaIncrementalSource.Error -= HohoemaIncrementalSource_Error;
                }
                oldItems.BeginLoading -= BeginLoadingItems;
                oldItems.DoneLoading -= CompleteLoadingItems;
                oldItems.Dispose();
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
                if (ItemsView?.Source is IncrementalLoadingCollection<IIncrementalSource<ITEM_VM>, ITEM_VM> oldItems)
                {
                    oldItems.StopLoading();
                }

                // Note: ListViewのItemTemplae内でUserControlを利用した場合のメモリリークバグを回避するListView.ItemsSourceにnullを与える
                ItemsView = null;
                RaisePropertyChanged(nameof(ItemsView));
            }

            base.OnNavigatedFrom(parameters);
        }

        private void DisposeItemsView(AdvancedCollectionView acv)
        {
            if (acv?.Source is IncrementalLoadingCollection<IIncrementalSource<ITEM_VM>, ITEM_VM> oldItems)
            {
                if (oldItems.Source is HohoemaIncrementalSourceBase<ITEM_VM> hohoemaIncrementalSource)
                {
                    hohoemaIncrementalSource.Error -= HohoemaIncrementalSource_Error;
                }
                oldItems.BeginLoading -= BeginLoadingItems;
                oldItems.DoneLoading -= CompleteLoadingItems;
                oldItems.Dispose();
            }
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
                    var source = GenerateIncrementalSource();

                    if (source == null)
                    {
                        HasItem.Value = false;
                        return;
                    }

                    MaxItemsCount.Value = await source.ResetSource(ct);

                    var items = new IncrementalLoadingCollection<IIncrementalSource<ITEM_VM>, ITEM_VM>(source);

                    items.BeginLoading += BeginLoadingItems;
                    items.DoneLoading += CompleteLoadingItems;

                    if (items.Source is HohoemaIncrementalSourceBase<ITEM_VM>)
                    {
                        (items.Source as HohoemaIncrementalSourceBase<ITEM_VM>).Error += HohoemaIncrementalSource_Error;
                    }

                    ItemsView = new AdvancedCollectionView(items);
                    RaisePropertyChanged(nameof(ItemsView));

                    //await ItemsView.LoadMoreItemsAsync(items.Source.OneTimeLoadCount);

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

			LoadedItemsCount.Value = ItemsView?.Count ?? 0;
			HasItem.Value = LoadedItemsCount.Value > 0;
        }

		protected virtual void PostResetList()
        {
            LatestUpdateTime = DateTime.Now;
        }

		protected abstract IIncrementalSource<ITEM_VM> GenerateIncrementalSource();

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
