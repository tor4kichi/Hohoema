﻿using Hohoema.Models.Domain.Niconico.Search;
using Hohoema.Models.Domain.PageNavigation;
using Hohoema.Models.Domain.Pins;
using Hohoema.Models.Domain.Subscriptions;
using Hohoema.Models.Helpers;
using Hohoema.Models.UseCase;
using Hohoema.Models.UseCase.Playlist;
using Hohoema.Models.UseCase.PageNavigation;
using Hohoema.Presentation.ViewModels.Niconico.Search;
using Hohoema.Presentation.ViewModels.Niconico.Video.Commands;
using Hohoema.Presentation.ViewModels.Subscriptions;
using Hohoema.Presentation.ViewModels.VideoListPage;
using Microsoft.Toolkit.Collections;
using NiconicoToolkit.SearchWithCeApi.Video;
using Prism.Commands;
using Prism.Navigation;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Hohoema.Models.Domain.Playlist;

namespace Hohoema.Presentation.ViewModels.Pages.Niconico.Search
{
    public class SearchResultKeywordPageViewModel : HohoemaListingPageViewModelBase<VideoListItemControlViewModel>, INavigatedAwareAsync, IPinablePage, ITitleUpdatablePage
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
        public SearchProvider SearchProvider { get; }
        public SubscriptionManager SubscriptionManager1 { get; }
        public PageManager PageManager { get; }
        public VideoPlayWithQueueCommand VideoPlayWithQueueCommand { get; }
        public PlaylistPlayAllCommand PlaylistPlayAllCommand { get; }
        public AddKeywordSearchSubscriptionCommand AddKeywordSearchSubscriptionCommand { get; }
        public SelectionModeToggleCommand SelectionModeToggleCommand { get; }
        public SubscriptionManager SubscriptionManager { get; }



        private CeApiSearchVideoPlaylist _SearchVideoPlaylist;
        public CeApiSearchVideoPlaylist SearchVideoPlaylist
        {
            get { return _SearchVideoPlaylist; }
            private set { SetProperty(ref _SearchVideoPlaylist, value); }
        }


        public CeApiSearchVideoPlaylistSortOption[] SortOptions => CeApiSearchVideoPlaylist.SortOptions;


        private CeApiSearchVideoPlaylistSortOption _selectedSortOption;
        public CeApiSearchVideoPlaylistSortOption SelectedSortOption
        {
            get { return _selectedSortOption; }
            set { SetProperty(ref _selectedSortOption, value); }
        }


        public ReadOnlyReactivePropertySlim<PlaylistToken> CurrentPlaylistToken { get; }


        public SearchResultKeywordPageViewModel(
            ApplicationLayoutManager applicationLayoutManager,
            SearchProvider searchProvider,
            SubscriptionManager subscriptionManager,
            PageManager pageManager,
            SearchHistoryRepository searchHistoryRepository,
            VideoPlayWithQueueCommand videoPlayWithQueueCommand,
            PlaylistPlayAllCommand playlistPlayAllCommand,
            AddKeywordSearchSubscriptionCommand addKeywordSearchSubscriptionCommand,
            SelectionModeToggleCommand selectionModeToggleCommand
            )
        {
            FailLoading = new ReactiveProperty<bool>(false)
                .AddTo(_CompositeDisposable);

            LoadedPage = new ReactiveProperty<int>(1)
                .AddTo(_CompositeDisposable);

            SelectedSearchTarget = new ReactiveProperty<SearchTarget>();


            ApplicationLayoutManager = applicationLayoutManager;
            SearchProvider = searchProvider;
            PageManager = pageManager;
            _searchHistoryRepository = searchHistoryRepository;
            VideoPlayWithQueueCommand = videoPlayWithQueueCommand;
            PlaylistPlayAllCommand = playlistPlayAllCommand;
            AddKeywordSearchSubscriptionCommand = addKeywordSearchSubscriptionCommand;
            SelectionModeToggleCommand = selectionModeToggleCommand;
            SubscriptionManager = subscriptionManager;

            CurrentPlaylistToken = Observable.CombineLatest(
                this.ObserveProperty(x => x.SearchVideoPlaylist),
                this.ObserveProperty(x => x.SelectedSortOption),
                (x, y) => new PlaylistToken(x, y)
                )
                .ToReadOnlyReactivePropertySlim()
                .AddTo(_CompositeDisposable);
        }



        static public List<SearchTarget> SearchTargets { get; } = Enum.GetValues(typeof(SearchTarget)).Cast<SearchTarget>().ToList();

        public ReactiveProperty<SearchTarget> SelectedSearchTarget { get; }

        private DelegateCommand<SearchTarget?> _ChangeSearchTargetCommand;
        public DelegateCommand<SearchTarget?> ChangeSearchTargetCommand
        {
            get
            {
                return _ChangeSearchTargetCommand
                    ?? (_ChangeSearchTargetCommand = new DelegateCommand<SearchTarget?>(target =>
                    {
                        if (target.HasValue && target.Value != SearchOption.SearchTarget)
                        {
                            PageManager.Search(target.Value, SearchOption.Keyword);
                        }
                    }));
            }
        }

        public ReactiveProperty<bool> FailLoading { get; private set; }

		static public KeywordSearchPagePayloadContent SearchOption { get; private set; }
		public ReactiveProperty<int> LoadedPage { get; private set; }

        private string _keyword;
        public string Keyword
        {
            get { return _keyword; }
            set { SetProperty(ref _keyword, value); }
        }

        #region Commands


        private DelegateCommand _ShowSearchHistoryCommand;
        private readonly SearchHistoryRepository _searchHistoryRepository;

        public DelegateCommand ShowSearchHistoryCommand
		{
			get
			{
				return _ShowSearchHistoryCommand
					?? (_ShowSearchHistoryCommand = new DelegateCommand(() =>
					{
						PageManager.OpenPage(HohoemaPageType.Search);
					}));
			}
		}

        #endregion

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

                SearchVideoPlaylist = new CeApiSearchVideoPlaylist(new PlaylistId() { Id = Keyword, Origin = PlaylistItemsSourceOrigin.SearchWithKeyword }, SearchProvider);
                SelectedSortOption = CeApiSearchVideoPlaylist.DefaultSortOption;

                this.ObserveProperty(x => x.SelectedSortOption)
                    .Subscribe(_ => ResetList())
                    .AddTo(_NavigatingCompositeDisposable);
            }


            SelectedSearchTarget.Value = SearchTarget.Keyword;


            _searchHistoryRepository.Searched(SearchOption.Keyword, SearchOption.SearchTarget);

            return base.OnNavigatedToAsync(parameters);
        }

        #region Implement HohoemaVideListViewModelBase

        protected override (int, IIncrementalSource<VideoListItemControlViewModel>) GenerateIncrementalSource()
		{
            return (VideoSearchIncrementalSource.OneTimeLoadingCount, new VideoSearchIncrementalSource(SearchVideoPlaylist, SelectedSortOption, SearchProvider));
		}
		

		protected override bool CheckNeedUpdateOnNavigateTo(NavigationMode mode)
		{
			if (ItemsView?.Source == null) { return true; }

            return base.CheckNeedUpdateOnNavigateTo(mode);
        }
        #endregion

    }    


    public enum VideoSearchMode
    {
        Keyword,
        Tag
    }
}
