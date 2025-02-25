﻿#nullable enable
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Hohoema.Models.Niconico;
using Hohoema.Models.Niconico.Search;
using Hohoema.Models.PageNavigation;
using Hohoema.Models.Pins;
using Hohoema.Models.Playlist;
using Hohoema.Models.Subscriptions;
using Hohoema.Services;
using Hohoema.ViewModels.Niconico.Search;
using Hohoema.ViewModels.Niconico.Video.Commands;
using Hohoema.ViewModels.Subscriptions;
using Hohoema.ViewModels.VideoListPage;
using I18NPortable;
using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.Collections;
using NiconicoToolkit.Search.Video;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Windows.UI.Xaml.Navigation;

namespace Hohoema.ViewModels.Pages.Niconico.Search;

public class SearchResultKeywordPageViewModel 
    : VideoListingPageViewModelBase<VideoListItemControlViewModel>
    , IPinablePage
    , ITitleUpdatablePage
{
    HohoemaPin IPinablePage.GetPin()
    {
        return new HohoemaPin()
        {
            Label = SearchOption.Keyword,
            PageType = HohoemaPageType.SearchResultKeyword,
            Parameter = $"keyword={System.Net.WebUtility.UrlEncode(SearchOption.Keyword)}&target={SearchOption.SearchTarget}"
        };
    }

    IObservable<string> ITitleUpdatablePage.GetTitleObservable()
    {
        return this.ObserveProperty(x => x.Keyword);
    }

    public ApplicationLayoutManager ApplicationLayoutManager { get; }    
    public VideoPlayWithQueueCommand VideoPlayWithQueueCommand { get; }
    public PlaylistPlayAllCommand PlaylistPlayAllCommand { get; }
    public AddKeywordSearchSubscriptionCommand AddKeywordSearchSubscriptionCommand { get; }
    public SelectionModeToggleCommand SelectionModeToggleCommand { get; }
    public SubscriptionManager SubscriptionManager { get; }



    private SearchVideoPlaylist _SearchVideoPlaylist;
    public SearchVideoPlaylist SearchVideoPlaylist
    {
        get { return _SearchVideoPlaylist; }
        private set { SetProperty(ref _SearchVideoPlaylist, value); }
    }


    public VideoSortOptionViewModel[] SortOptions { get; } = SearchVideoPlaylist.SortOptions.Select(x => new VideoSortOptionViewModel(x)).ToArray();

    private VideoSortOptionViewModel _selectedSortOption;
    public VideoSortOptionViewModel SelectedSortOption
    {
        get { return _selectedSortOption; }
        set { SetProperty(ref _selectedSortOption, value); }
    }

    private VideoSortOptionViewModel DefaultSortOptionVM => SortOptions.First(x => x.SortOption == SearchVideoPlaylist.DefaultSortOption);


    public ReadOnlyReactivePropertySlim<PlaylistToken?> CurrentPlaylistToken { get; }


    public SearchResultKeywordPageViewModel(
        IMessenger messenger,
        ILoggerFactory loggerFactory,
        NiconicoSession niconicoSession,
        ApplicationLayoutManager applicationLayoutManager,
        SearchProvider searchProvider,
        SubscriptionManager subscriptionManager,        
        SearchHistoryRepository searchHistoryRepository,
        VideoPlayWithQueueCommand videoPlayWithQueueCommand,
        PlaylistPlayAllCommand playlistPlayAllCommand,
        AddKeywordSearchSubscriptionCommand addKeywordSearchSubscriptionCommand,
        SelectionModeToggleCommand selectionModeToggleCommand
        )
        : base(messenger, loggerFactory.CreateLogger<SearchResultKeywordPageViewModel>(), disposeItemVM: false)
    {
        FailLoading = new ReactiveProperty<bool>(false)
            .AddTo(_CompositeDisposable);

        LoadedPage = new ReactiveProperty<int>(1)
            .AddTo(_CompositeDisposable);

        SelectedSearchTarget = new ReactiveProperty<SearchTarget>();
        _niconicoSession = niconicoSession;
        ApplicationLayoutManager = applicationLayoutManager;        
        _searchHistoryRepository = searchHistoryRepository;
        VideoPlayWithQueueCommand = videoPlayWithQueueCommand;
        PlaylistPlayAllCommand = playlistPlayAllCommand;
        AddKeywordSearchSubscriptionCommand = addKeywordSearchSubscriptionCommand;
        SelectionModeToggleCommand = selectionModeToggleCommand;
        SubscriptionManager = subscriptionManager;

        CurrentPlaylistToken = Observable.CombineLatest(
            this.ObserveProperty(x => x.SearchVideoPlaylist),
            this.ObserveProperty(x => x.SelectedSortOption).Where(x => x is not null),
            (x, y) => new PlaylistToken(x, y.SortOption)
            )
            .ToReadOnlyReactivePropertySlim()
            .AddTo(_CompositeDisposable);
    }



    static public List<SearchTarget> SearchTargets { get; } = Enum.GetValues(typeof(SearchTarget)).Cast<SearchTarget>().ToList();

    public ReactiveProperty<SearchTarget> SelectedSearchTarget { get; }

    private RelayCommand<SearchTarget?> _ChangeSearchTargetCommand;
    public RelayCommand<SearchTarget?> ChangeSearchTargetCommand =>
        _ChangeSearchTargetCommand ??= new RelayCommand<SearchTarget?>(target =>
        {
            if (target.HasValue && target.Value != SearchOption.SearchTarget)
            {
                _ = _messenger.OpenSearchPageAsync(target.Value, SearchOption.Keyword);
            }
        });

    static public KeywordSearchPagePayloadContent SearchOption { get; private set; }
    public ReactiveProperty<bool> FailLoading { get; private set; }
    public ReactiveProperty<int> LoadedPage { get; private set; }

    private string _keyword;
    public string Keyword
    {
        get { return _keyword; }
        set { SetProperty(ref _keyword, value); }
    }

    private readonly NiconicoSession _niconicoSession;
    private readonly SearchHistoryRepository _searchHistoryRepository;


    private RelayCommand _ShowSearchHistoryCommand;
    public RelayCommand ShowSearchHistoryCommand =>
        _ShowSearchHistoryCommand ??= new RelayCommand(() =>
        {
            _ = _messenger.OpenPageAsync(HohoemaPageType.Search);
        });

    public override Task OnNavigatedToAsync(INavigationParameters parameters)
    {
        var mode = parameters.GetNavigationMode();
        if (mode == NavigationMode.New)
        {
            Keyword = System.Net.WebUtility.UrlDecode(parameters.GetValue<string>("keyword"));

            SearchOption = new KeywordSearchPagePayloadContent()
            {
                Keyword = Keyword
            };

            SearchVideoPlaylist = new SearchVideoPlaylist(new PlaylistId() { Id = Keyword, Origin = PlaylistItemsSourceOrigin.SearchWithKeyword }, _niconicoSession.ToolkitContext.Search);
            SelectedSortOption = DefaultSortOptionVM;

            this.ObserveProperty(x => x.SelectedSortOption)
                .Subscribe(_ => ResetList())
                .AddTo(_navigationDisposables);
        }

        Title = $"{"Search".Translate()} '{Keyword}'";

        SelectedSearchTarget.Value = SearchTarget.Keyword;


        _searchHistoryRepository.Searched(SearchOption.Keyword, SearchOption.SearchTarget);

        return base.OnNavigatedToAsync(parameters);
    }

    #region Implement HohoemaVideListViewModelBase

    protected override (int, IIncrementalSource<VideoListItemControlViewModel>) GenerateIncrementalSource()
    {
        if (_selectedSortOption is null)
        {
            SelectedSortOption = DefaultSortOptionVM;
        }

        return (
            VideoSearchIncrementalSource.OneTimeLoadingCount, 
            new VideoSearchIncrementalSource(
                _niconicoSession.ToolkitContext.Search, 
                Keyword, 
                isTagSearch: false, 
                SelectedSortOption.SortKey,
                SelectedSortOption.SortOrder
                ));
    }


    protected override bool CheckNeedUpdateOnNavigateTo(NavigationMode mode, INavigationParameters parameters)
    {
        if (ItemsView?.Source == null) { return true; }

        return base.CheckNeedUpdateOnNavigateTo(mode, parameters);
    }
    #endregion

}


public enum VideoSearchMode
{
    Keyword,
    Tag
}

public sealed class VideoSortOptionViewModel
{
    public VideoSortOptionViewModel(SearchVideoPlaylistSortOption sortOption)
    {
        SortOption = sortOption;
        SortKey = SortOption.SortKey;
        SortOrder = SortOption.SortOrder;
        if (SortKey is SortKey.Hot or SortKey.Personalized)
        {
            Label = $"VideoSortKey.{SortKey}".Translate();
        }
        else
        {
            Label = $"VideoSortKey.{SortKey}_{SortOrder}".Translate();
        }
    }

    public SortKey SortKey { get; }
    public SortOrder SortOrder { get; }

    public string Label { get; }
    public SearchVideoPlaylistSortOption SortOption { get; }
}