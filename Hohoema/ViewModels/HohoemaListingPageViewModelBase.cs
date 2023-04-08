using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.Collections;
using Microsoft.Toolkit.Uwp;
using Microsoft.Toolkit.Uwp.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI.Xaml.Navigation;
using ZLogger;

#nullable enable

namespace Hohoema.ViewModels;

public abstract partial class HohoemaListingPageViewModelBase<ITEM_VM> : HohoemaPageViewModelBase
{

    /// <summary>
    /// ListViewBaseが初回表示時に複数回取得動作を実行してしまう問題を回避するためのワークアラウンド
    /// １回目の取得内容を２分割して２回に分けて届ける。３回目以降はそのまま取得して返すだけ。
    /// </summary>
    /// <remarks>ListViewBase.IncrementalLoadingThreshold が未指定でないと動作しない</remarks>
    public class HohoemaIncrementalLoadingCollection : IncrementalLoadingCollection<IIncrementalSource<ITEM_VM>, ITEM_VM>
    {
        public HohoemaIncrementalLoadingCollection(int itemsPerPage = 20, Action? onStartLoading = null, Action? onEndLoading = null, Action<Exception>? onError = null) : base(itemsPerPage, onStartLoading, onEndLoading, onError)
        {
        }

        public HohoemaIncrementalLoadingCollection(IIncrementalSource<ITEM_VM> source, int itemsPerPage = 20, Action? onStartLoading = null, Action? onEndLoading = null, Action<Exception>? onError = null) : base(source, itemsPerPage, onStartLoading, onEndLoading, onError)
        {
        }

        public new int ItemsPerPage => base.ItemsPerPage;

        int _timing = 0;
        List<ITEM_VM>? _dividedPresentItemsSource;
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


    [ObservableProperty]
    private int _maxItemsCount;

    [ObservableProperty]
    private int _loadedItemsCount;


    public AdvancedCollectionView? ItemsView { get; private set; }

    [ObservableProperty]
    private bool _nowLoading;

    partial void OnNowLoadingChanged(bool value)
    {
        CanChangeSort = !value;
    }

    [ObservableProperty]
    private bool _canChangeSort;

    [ObservableProperty]
    private bool _hasItem;

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private Exception? _error;


    public DateTime LatestUpdateTime = DateTime.Now;


    private readonly DispatcherQueue _dispatcherQueue;
    protected readonly ILogger _logger;


    public HohoemaListingPageViewModelBase(ILogger logger)
    {        
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
        _lastParameterForError = parameters;
        base.OnNavigatedTo(parameters);
    }

    INavigationParameters _lastParameterForError;
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

    private void DisposeItemsView(AdvancedCollectionView? acv)
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
        NowLoading = true;
        HasItem = true;
        HasError = false;
        LoadedItemsCount = 0;

        var prevItemsView = ItemsView;
        ItemsView = null;
        OnPropertyChanged(nameof(ItemsView));

        DisposeItemsView(prevItemsView);

        try
        {
            var (pageSize, source) = GenerateIncrementalSource();

            if (source == null)
            {
                HasItem = false;
                return;
            }

            var items = new HohoemaIncrementalLoadingCollection(source, pageSize, BeginLoadingItems, onEndLoading: CompleteLoadingItems, OnLodingItemError);

            ItemsView = new AdvancedCollectionView(items);
            OnPropertyChanged(nameof(ItemsView));

            PostResetList();
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            HasError = true;
            var viewModelName = this.GetType().Name;
            var viewModelNameWoPostfix = viewModelName.Remove(viewModelName.Length - "PageViewModel".Length);
            string parameters = _lastParameterForError.Select(x => new KeyValuePair<string, string>(x.Key, x.Value.ToString())).Where(x => x.Key != "__nm").ToQueryStringWithoutEscape();
            Error = new HohoemaVideoListException(viewModelNameWoPostfix, parameters, e);

            _logger.ZLogError(Error, "failed GenerateIncrementalSource.");            
        }
        finally
        {
            NowLoading = false;
        }
    }



    private void OnLodingItemError(Exception e)
    {
        _logger.ZLogError(e, "failed on incremental loadingItems.");
    }

    [RelayCommand]
    protected void ResetList()
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            ResetList_Internal(NavigationCancellationToken);
        });
    }


    private void BeginLoadingItems()
    {
        HasError = false;
        Error = null;
        NowLoading = true;
    }

    private void CompleteLoadingItems()
    {
        NowLoading = false;
        LoadedItemsCount = ItemsView?.Count ?? 0;
        HasItem = LoadedItemsCount > 0 || (ItemsView?.HasMoreItems ?? false);

        // ページのキャッシュ非使用かつViewModelをキャッシュしている場合に
        // ２回目の読み込み時だけ LoadedItemsCount.Value == 0 となり読み込みが発生しないため
        if (HasItem is false)
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

    
    private RelayCommand<string>? _SortAscendingCommand;
    public RelayCommand<string> SortAscendingCommand
    {
        get
        {
            return _SortAscendingCommand ??= 
                new RelayCommand<string>(propertyName =>
                {
                    AddSortDescription(new SortDescription(propertyName, SortDirection.Ascending), withReset: true);
                });
        }
    }

    private RelayCommand<string>? _SortDescendingCommand;
    public RelayCommand<string> SortDescendingCommand
    {
        get
        {
            return _SortDescendingCommand ??= 
                new RelayCommand<string>(propertyName =>
                {
                    AddSortDescription(new SortDescription(propertyName, SortDirection.Descending), withReset: true);
                });
        }
    }



    protected void AddSortDescription(SortDescription sort, bool withReset = false)
    {
        Guard.IsNotNull(ItemsView);
        if (withReset)
        {
            ItemsView.SortDescriptions.Clear();
        }

        ItemsView.SortDescriptions.Add(sort);
        ItemsView.RefreshSorting();
    }

    [RelayCommand]
    protected void ResetSort()
    {
        Guard.IsNotNull(ItemsView);
        ItemsView.SortDescriptions.Clear();
        ItemsView.RefreshSorting();
    }


    protected void AddFilter(Predicate<ITEM_VM> predicate)
    {
        Guard.IsNotNull(ItemsView);
        ItemsView.Filter = (p) => predicate((ITEM_VM)p);
        ItemsView.RefreshFilter();
    }

    protected void ResetFilter()
    {
        Guard.IsNotNull(ItemsView);
        ItemsView.Filter = null;
        ItemsView.RefreshFilter();
    }    
}