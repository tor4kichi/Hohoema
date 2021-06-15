using Hohoema.Models.Domain.Niconico.Search;
using Hohoema.Models.Domain.PageNavigation;
using Hohoema.Models.Domain.Pins;
using Hohoema.Models.Domain.Subscriptions;
using Hohoema.Models.Helpers;
using Hohoema.Models.UseCase;
using Hohoema.Models.UseCase.NicoVideos;
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
        public HohoemaPlaylist HohoemaPlaylist { get; }
        public PageManager PageManager { get; }
        public AddKeywordSearchSubscriptionCommand AddKeywordSearchSubscriptionCommand { get; }
        public SelectionModeToggleCommand SelectionModeToggleCommand { get; }
        public SubscriptionManager SubscriptionManager { get; }

        public SearchResultKeywordPageViewModel(
            ApplicationLayoutManager applicationLayoutManager,
            SearchProvider searchProvider,
            SubscriptionManager subscriptionManager,
            HohoemaPlaylist hohoemaPlaylist,
            PageManager pageManager,
            SearchHistoryRepository searchHistoryRepository,
            AddKeywordSearchSubscriptionCommand addKeywordSearchSubscriptionCommand,
            SelectionModeToggleCommand selectionModeToggleCommand
            )
        {
            FailLoading = new ReactiveProperty<bool>(false)
                .AddTo(_CompositeDisposable);

            LoadedPage = new ReactiveProperty<int>(1)
                .AddTo(_CompositeDisposable);

            SelectedSearchSort = new ReactiveProperty<SearchSortOptionListItem>(
                VideoSearchOptionListItems.First(),
                mode: ReactivePropertyMode.DistinctUntilChanged
                );

            SelectedSearchTarget = new ReactiveProperty<SearchTarget>();

            SelectedSearchSort
               .Subscribe(async _ =>
               {
                   var selected = SelectedSearchSort.Value;
                   if (SearchOption.Order == selected.Order
                       && SearchOption.Sort == selected.Sort
                   )
                   {
                       //                       return;
                   }

                   SearchOption.Sort = SelectedSearchSort.Value.Sort;
                   SearchOption.Order = SelectedSearchSort.Value.Order;

                   ResetList();
               })
                .AddTo(_CompositeDisposable);

            ApplicationLayoutManager = applicationLayoutManager;
            SearchProvider = searchProvider;
            HohoemaPlaylist = hohoemaPlaylist;
            PageManager = pageManager;
            _searchHistoryRepository = searchHistoryRepository;
            AddKeywordSearchSubscriptionCommand = addKeywordSearchSubscriptionCommand;
            SelectionModeToggleCommand = selectionModeToggleCommand;
            SubscriptionManager = subscriptionManager;
        }


        static private List<SearchSortOptionListItem> _VideoSearchOptionListItems = new List<SearchSortOptionListItem>()
        {
            new SearchSortOptionListItem()
            {
                Label = SortHelper.ToCulturizedText(VideoSortKey.FirstRetrieve, VideoSortOrder.Desc),
                Order = VideoSortOrder.Desc,
                Sort = VideoSortKey.FirstRetrieve,
            },
            new SearchSortOptionListItem()
            {
                Label = SortHelper.ToCulturizedText(VideoSortKey.FirstRetrieve, VideoSortOrder.Asc),
                Order = VideoSortOrder.Asc,
                Sort = VideoSortKey.FirstRetrieve,
            },

            new SearchSortOptionListItem()
            {
                Label = SortHelper.ToCulturizedText(VideoSortKey.NewComment, VideoSortOrder.Desc),
                Order = VideoSortOrder.Desc,
                Sort = VideoSortKey.NewComment,
            },
            new SearchSortOptionListItem()
            {
                Label = SortHelper.ToCulturizedText(VideoSortKey.NewComment, VideoSortOrder.Asc),
                Order = VideoSortOrder.Asc,
                Sort = VideoSortKey.NewComment,
            },

            new SearchSortOptionListItem()
            {
                Label = SortHelper.ToCulturizedText(VideoSortKey.ViewCount, VideoSortOrder.Desc),
                Order = VideoSortOrder.Desc,
                Sort = VideoSortKey.ViewCount,
            },
            new SearchSortOptionListItem()
            {
                Label = SortHelper.ToCulturizedText(VideoSortKey.ViewCount, VideoSortOrder.Asc),
                Order = VideoSortOrder.Asc,
                Sort = VideoSortKey.ViewCount,
            },

            new SearchSortOptionListItem()
            {
                Label = SortHelper.ToCulturizedText(VideoSortKey.CommentCount, VideoSortOrder.Desc),
                Order = VideoSortOrder.Desc,
                Sort = VideoSortKey.CommentCount,
            },
            new SearchSortOptionListItem()
            {
                Label = SortHelper.ToCulturizedText(VideoSortKey.CommentCount, VideoSortOrder.Asc),
                Order = VideoSortOrder.Asc,
                Sort = VideoSortKey.CommentCount,
            },


            new SearchSortOptionListItem()
            {
                Label = SortHelper.ToCulturizedText(VideoSortKey.Length, VideoSortOrder.Desc),
                Order = VideoSortOrder.Desc,
                Sort = VideoSortKey.Length,
            },
            new SearchSortOptionListItem()
            {
                Label = SortHelper.ToCulturizedText(VideoSortKey.Length, VideoSortOrder.Asc),
                Order = VideoSortOrder.Asc,
                Sort = VideoSortKey.Length,
            },

            new SearchSortOptionListItem()
            {
                Label = SortHelper.ToCulturizedText(VideoSortKey.MylistCount, VideoSortOrder.Desc),
                Order = VideoSortOrder.Desc,
                Sort = VideoSortKey.MylistCount,
            },
            new SearchSortOptionListItem()
            {
                Label = SortHelper.ToCulturizedText(VideoSortKey.MylistCount, VideoSortOrder.Asc),
                Order = VideoSortOrder.Asc,
                Sort = VideoSortKey.MylistCount,
            },
		};

        public IReadOnlyList<SearchSortOptionListItem> VideoSearchOptionListItems => _VideoSearchOptionListItems;

        public ReactiveProperty<SearchSortOptionListItem> SelectedSearchSort { get; private set; }


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


        private string _SearchOptionText;
        public string SearchOptionText
        {
            get { return _SearchOptionText; }
            set { SetProperty(ref _SearchOptionText, value); }
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
            }


            SelectedSearchTarget.Value = SearchTarget.Keyword;

            SelectedSearchSort.Value = VideoSearchOptionListItems.First(x => x.Sort == SearchOption.Sort && x.Order == SearchOption.Order);


            _searchHistoryRepository.Searched(SearchOption.Keyword, SearchOption.SearchTarget);

            return base.OnNavigatedToAsync(parameters);
        }

        #region Implement HohoemaVideListViewModelBase

        protected override (int, IIncrementalSource<VideoListItemControlViewModel>) GenerateIncrementalSource()
		{
            return (VideoSearchIncrementalSource.OneTimeLoadingCount, new VideoSearchIncrementalSource(SearchOption.Keyword, false, SearchOption.Sort, SearchOption.Order, SearchProvider));
		}

		protected override void PostResetList()
		{
            SearchOptionText = SortHelper.ToCulturizedText(SearchOption.Sort, SearchOption.Order);
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
