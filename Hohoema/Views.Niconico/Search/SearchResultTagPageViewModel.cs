using Hohoema.Models.Niconico;
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
using CommunityToolkit.Mvvm.Input;
using Hohoema.Services.Navigations;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using NiconicoSession = Hohoema.Models.Niconico.NiconicoSession;
using Hohoema.ViewModels.Niconico.Follow;
using Hohoema.Models.Niconico.Follow.LoginUser;
using Hohoema.Models.Niconico.Video;
using NiconicoToolkit.SearchWithCeApi.Video;
using Microsoft.Toolkit.Collections;
using Hohoema.Models.Playlist;
using Microsoft.Extensions.Logging;
using Windows.UI.Xaml.Navigation;
using I18NPortable;

namespace Hohoema.ViewModels.Pages.Niconico.Search
{
    using TagFollowContext = FollowContext<Models.Niconico.Video.ITag>;

    public class SearchResultTagPageViewModel : HohoemaListingPageViewModelBase<VideoListItemControlViewModel>, ITag, IPinablePage, ITitleUpdatablePage
    {
        HohoemaPin IPinablePage.GetPin()
        {
            return new HohoemaPin()
            {
                Label = SearchOption.Keyword,
                PageType = HohoemaPageType.SearchResultTag,
                Parameter = $"keyword={System.Net.WebUtility.UrlEncode(SearchOption.Keyword)}&target={SearchOption.SearchTarget}"
            };
        }

        IObservable<string> ITitleUpdatablePage.GetTitleObservable()
        {
            return this.ObserveProperty(x => x.Keyword);
        }

        public SearchResultTagPageViewModel(
            ILoggerFactory loggerFactory,
            ApplicationLayoutManager applicationLayoutManager,
            NiconicoSession niconicoSession,
            SearchProvider searchProvider,
            TagFollowProvider tagFollowProvider,
            SubscriptionManager subscriptionManager,
            PageManager pageManager,
            SearchHistoryRepository searchHistoryRepository,
            Services.DialogService dialogService,
            VideoPlayWithQueueCommand videoPlayWithQueueCommand,
            PlaylistPlayAllCommand playlistPlayAllCommand,
            AddTagSearchSubscriptionCommand addTagSearchSubscriptionCommand,
            SelectionModeToggleCommand selectionModeToggleCommand
            )
            : base(loggerFactory.CreateLogger<SearchResultTagPageViewModel>())
        {
            SearchProvider = searchProvider;
            _tagFollowProvider = tagFollowProvider;
            SubscriptionManager = subscriptionManager;
            PageManager = pageManager;
            _searchHistoryRepository = searchHistoryRepository;
            ApplicationLayoutManager = applicationLayoutManager;
            NiconicoSession = niconicoSession;
            HohoemaDialogService = dialogService;
            VideoPlayWithQueueCommand = videoPlayWithQueueCommand;
            PlaylistPlayAllCommand = playlistPlayAllCommand;
            AddTagSearchSubscriptionCommand = addTagSearchSubscriptionCommand;
            SelectionModeToggleCommand = selectionModeToggleCommand;
            FailLoading = new ReactiveProperty<bool>(false)
                .AddTo(_CompositeDisposable);

            LoadedPage = new ReactiveProperty<int>(1)
                .AddTo(_CompositeDisposable);

            SelectedSearchTarget = new ReactiveProperty<SearchTarget>();


            CurrentPlaylistToken = Observable.CombineLatest(
                this.ObserveProperty(x => x.SearchVideoPlaylist),
                this.ObserveProperty(x => x.SelectedSortOption),
                (x, y) => new PlaylistToken(x, y)
                )
                .ToReadOnlyReactivePropertySlim()
                .AddTo(_CompositeDisposable);
        }

        public ApplicationLayoutManager ApplicationLayoutManager { get; }
        public NiconicoSession NiconicoSession { get; }
        public SearchProvider SearchProvider { get; }
        public SubscriptionManager SubscriptionManager { get; }
        public PageManager PageManager { get; }
        public Services.DialogService HohoemaDialogService { get; }
        public VideoPlayWithQueueCommand VideoPlayWithQueueCommand { get; }
        public PlaylistPlayAllCommand PlaylistPlayAllCommand { get; }
        public AddTagSearchSubscriptionCommand AddTagSearchSubscriptionCommand { get; }
        public SelectionModeToggleCommand SelectionModeToggleCommand { get; }





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




        private string _keyword;
        public string Keyword
        {
            get { return _keyword; }
            set { SetProperty(ref _keyword, value); }
        }

		public ReactiveProperty<bool> FailLoading { get; private set; }

        static public TagSearchPagePayloadContent SearchOption { get; private set; }
		public ReactiveProperty<int> LoadedPage { get; private set; }


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


        // Follow
        private TagFollowContext _FollowContext = TagFollowContext.Default;
        public TagFollowContext FollowContext
        {
            get => _FollowContext;
            set => SetProperty(ref _FollowContext, value);
        }



        #region Commands


        private RelayCommand _ShowSearchHistoryCommand;
        private readonly TagFollowProvider _tagFollowProvider;
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

        string ITag.Tag => SearchOption.Keyword;

        #endregion

        public override async Task OnNavigatedToAsync(INavigationParameters parameters)
        {
            var mode = parameters.GetNavigationMode();
            if (mode == NavigationMode.New)
            {
                Keyword = System.Net.WebUtility.UrlDecode(parameters.GetValue<string>("keyword"));

                SearchOption = new TagSearchPagePayloadContent()
                {
                    Keyword = Keyword
                };

                SearchVideoPlaylist = new CeApiSearchVideoPlaylist(new PlaylistId() { Id = Keyword, Origin = PlaylistItemsSourceOrigin.SearchWithTag }, SearchProvider);
                SelectedSortOption = CeApiSearchVideoPlaylist.DefaultSortOption;

                this.ObserveProperty(x => x.SelectedSortOption)
                    .Subscribe(_ => ResetList())
                    .AddTo(_navigationDisposables);

            }

            Title = $"{"Search".Translate()} '{Keyword}'";

            SelectedSearchTarget.Value = SearchTarget.Tag;

            _searchHistoryRepository.Searched(SearchOption.Keyword, SearchOption.SearchTarget);

            try
            {
                if (NiconicoSession.IsLoggedIn && !string.IsNullOrWhiteSpace(Keyword))
                {
                    FollowContext = await TagFollowContext.CreateAsync(_tagFollowProvider, this);
                }
                else
                {
                    FollowContext = TagFollowContext.Default;
                }
            }
            catch
            {
                FollowContext = TagFollowContext.Default;
            }

            await base.OnNavigatedToAsync(parameters);
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
}
