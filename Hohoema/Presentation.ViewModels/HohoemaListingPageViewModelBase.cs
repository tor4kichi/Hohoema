using Hohoema.Models.Helpers;
using CommunityToolkit.Mvvm.Input;
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
using System.Reactive.Concurrency;
using Windows.System;
using Hohoema.Models.UseCase;
using Microsoft.Toolkit.Collections;
using Microsoft.Toolkit.Uwp;
using Microsoft.Extensions.Logging;
using ZLogger;
using Windows.UI.Xaml.Navigation;
using Hohoema.Presentation.Navigations;

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

            public new int ItemsPerPage => base.ItemsPerPage;
            
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




        public ReactiveProperty<int> MaxItemsCount { get; }
        public ReactiveProperty<int> LoadedItemsCount { get; }

        public AdvancedCollectionView ItemsView { get; private set; }

        public ReactiveProperty<bool> NowLoading { get; }
        public ReactiveProperty<bool> CanChangeSort { get; }

        public ReactiveProperty<bool> NowRefreshable { get;}

        public ReactiveProperty<bool> HasItem { get; }

        public ReactiveProperty<bool> HasError { get; }

                
        public DateTime LatestUpdateTime = DateTime.Now;


        private readonly DispatcherQueue _dispatcherQueue;
        protected readonly ILogger _logger;


        public HohoemaListingPageViewModelBase(ILogger logger)
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

            NowRefreshable = new ReactiveProperty<bool>(false)
                .AddTo(_CompositeDisposable);

            // 読み込み中または選択中はソートを変更できない
            CanChangeSort = Observable.CombineLatest(
                NowLoading
                )
                .Select(x => !x.Any(y => y))
                .ToReactiveProperty()
                .AddTo(_CompositeDisposable);

            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _logger = logger;
        }


        public override void Dispose()
        {
            if (ItemsView?.Source is IncrementalLoadingCollection<IIncrementalSource<ITEM_VM>, ITEM_VM> oldItems)
            {
                DisposeItemsView(ItemsView);
                ItemsView.Source = new List<ITEM_VM>();
                ItemsView.Clear();
                ItemsView = null;
                OnPropertyChanged(nameof(ItemsView));
            }

            base.Dispose();
        }


        public override void OnNavigatingTo(INavigationParameters parameters)
        {
            var navigationMode = parameters.GetNavigationMode();
            if (CheckNeedUpdateOnNavigateTo(navigationMode, parameters))
            {
                DisposeItemsView(ItemsView);
                ItemsView = null;
            }

            base.OnNavigatingTo(parameters);
        }

        public override void OnNavigatedTo(INavigationParameters parameters)
        {
            base.OnNavigatedTo(parameters);
        }
        
        public override Task OnNavigatedToAsync(INavigationParameters parameters)
        {
            if (ItemsView == null)
            {
                try
                {
                    ResetList_Internal(NavigationCancellationToken);
                }
                catch (OperationCanceledException)
                {

                }
                catch (Exception e)
                {
                    _logger.ZLogError(e, "failed GenerateIncrementalSource.");
                }                
            }

            return Task.CompletedTask;
        }

        public override void OnNavigatedFrom(INavigationParameters parameters)
        {            
            base.OnNavigatedFrom(parameters);
        }

        private void DisposeItemsView(AdvancedCollectionView acv)
        {
            if (acv == null) { return; }

            foreach (var item in acv)
            {
                (item as IDisposable)?.Dispose();
            }

            (acv?.Source as IDisposable)?.Dispose();
            (acv as IDisposable)?.Dispose();
        }

        private void ResetList_Internal(CancellationToken ct)
        {
            var prevItemsView = ItemsView;
            ItemsView = null;
            OnPropertyChanged(nameof(ItemsView));

            NowLoading.Value = true;
            HasItem.Value = true;
            HasError.Value = false;
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
                OnPropertyChanged(nameof(ItemsView));

                PostResetList();
            }
            catch
            {
                HasError.Value = true;
                throw;
            }
            finally
            {
                NowLoading.Value = false;
            }
            
        }

        private void OnLodingItemError(Exception e)
        {
            //ErrorTrackingManager.TrackError(e);
        }

        protected void ResetList()
        {
            _dispatcherQueue.TryEnqueue(() => 
            {
                try
                {
                    ResetList_Internal(NavigationCancellationToken);
                }
                catch (OperationCanceledException)
                {

                }
                catch (Exception e)
                {
                    _logger.ZLogError(e, "failed GenerateIncrementalSource.");
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
            HasItem.Value = LoadedItemsCount.Value > 0 || (ItemsView?.HasMoreItems ?? false);

            // ページのキャッシュ非使用かつViewModelをキャッシュしている場合に
            // ２回目の読み込み時だけ LoadedItemsCount.Value == 0 となり読み込みが発生しないため
            if (HasItem.Value is false)
            {
                //_ = ItemsView.LoadMoreItemsAsync((uint)(ItemsView.Source as HohoemaIncrementalLoadingCollection).ItemsPerPage);
            }
        }

		protected virtual void PostResetList()
        {
            LatestUpdateTime = DateTime.Now;
        }

		protected abstract (int PageSize, IIncrementalSource<ITEM_VM> IncrementalSource) GenerateIncrementalSource();

		protected virtual bool CheckNeedUpdateOnNavigateTo(NavigationMode mode, INavigationParameters parameters)
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

        private RelayCommand _ResetSortCommand;
        public RelayCommand ResetSortCommand
        {
            get
            {
                return _ResetSortCommand
                    ?? (_ResetSortCommand = new RelayCommand(() =>
                    {
                        ResetSort();
                    }
                    ));
            }
        }

        private RelayCommand<string> _SortAscendingCommand;
        public RelayCommand<string> SortAscendingCommand
        {
            get
            {
                return _SortAscendingCommand
                    ?? (_SortAscendingCommand = new RelayCommand<string>(propertyName => 
                    {
                        AddSortDescription(new SortDescription(propertyName, SortDirection.Ascending), withReset:true);
                    }
                    ));
            }
        }

        private RelayCommand<string> _SortDescendingCommand;
        public RelayCommand<string> SortDescendingCommand
        {
            get
            {
                return _SortDescendingCommand
                    ?? (_SortDescendingCommand = new RelayCommand<string>(propertyName =>
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

        
        private RelayCommand _RefreshCommand;
        public RelayCommand RefreshCommand
		{
			get
			{
				return _RefreshCommand
					?? (_RefreshCommand = new RelayCommand(() => 
					{
                        ResetList();
                    }));
			}
		}
	}
}
