using Hohoema.Models.Niconico.Search;
using Hohoema.Models.PageNavigation;
using Hohoema.Models.Pins;
using Hohoema.Models.Subscriptions;
using Hohoema.Helpers;
using Hohoema.Services;
using Hohoema.Services.Playlist;
using Hohoema.Services.Navigations;
using Hohoema.ViewModels.Niconico.Search;
using Hohoema.ViewModels.Niconico.Video.Commands;
using Hohoema.ViewModels.Subscriptions;
using Hohoema.ViewModels.VideoListPage;
using Microsoft.Toolkit.Collections;
using NiconicoToolkit.SearchWithCeApi.Video;
using CommunityToolkit.Mvvm.Input;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Hohoema.Models.Playlist;
using Microsoft.Extensions.Logging;
using Hohoema.Services.Navigations;
using Windows.UI.Xaml.Navigation;
using I18NPortable;

namespace Hohoema.ViewModels.Pages.Niconico.Search
{
    public class SearchResultKeywordPageViewModel : HohoemaListingPageViewModelBase<VideoListItemControlViewModel>, IPinablePage, ITitleUpdatablePage
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
            ILoggerFactory loggerFactory,
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
            : base(loggerFactory.CreateLogger<SearchResultKeywordPageViewModel>())
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

        private RelayCommand<SearchTarget?> _ChangeSearchTargetCommand;
        public RelayCommand<SearchTarget?> ChangeSearchTargetCommand
        {
            get
            {
                return _ChangeSearchTargetCommand
                    ?? (_ChangeSearchTargetCommand = new RelayCommand<SearchTarget?>(target =>
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


        private RelayCommand _ShowSearchHistoryCommand;
        private readonly SearchHistoryRepository _searchHistoryRepository;

        public RelayCommand ShowSearchHistoryCommand
		{
			get
			{
				return _ShowSearchHistoryCommand
					?? (_ShowSearchHistoryCommand = new RelayCommand(() =>
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
                SelectedSortOption = CeApiSearchVideoPlaylist.DefaultSortOption;
            }

            return (VideoSearchIncrementalSource.OneTimeLoadingCount, new VideoSearchIncrementalSource(SearchVideoPlaylist, SelectedSortOption, SearchProvider));
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
}
