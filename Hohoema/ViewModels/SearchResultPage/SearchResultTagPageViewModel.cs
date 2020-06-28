﻿using Mntone.Nico2;
using Hohoema.Interfaces;
using Hohoema.Models;
using Hohoema.Models.Helpers;
using Hohoema.Models.Provider;
using Hohoema.Models.Subscription;
using Hohoema.Services;
using Hohoema.Services.Page;
using Hohoema.UseCase;
using Hohoema.UseCase.Playlist;
using Hohoema.ViewModels.Subscriptions;
using Prism.Commands;
using Prism.Navigation;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;


namespace Hohoema.ViewModels
{
    
	public class SearchResultTagPageViewModel : HohoemaListingPageViewModelBase<VideoInfoControlViewModel>, Interfaces.ISearchWithtag, INavigatedAwareAsync, IPinablePage, ITitleUpdatablePage
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
            ApplicationLayoutManager applicationLayoutManager,
            NGSettings ngSettings,
            Models.NiconicoSession niconicoSession,
            SearchProvider searchProvider,
            SubscriptionManager subscriptionManager,
            HohoemaPlaylist hohoemaPlaylist,
            Services.PageManager pageManager,
            Services.DialogService dialogService,
            ViewModels.Subscriptions.AddTagSearchSubscriptionCommand addTagSearchSubscriptionCommand,
            NiconicoFollowToggleButtonService followButtonService
            )
        {
            SearchProvider = searchProvider;
            SubscriptionManager = subscriptionManager;
            HohoemaPlaylist = hohoemaPlaylist;
            PageManager = pageManager;
            ApplicationLayoutManager = applicationLayoutManager;
            NgSettings = ngSettings;
            NiconicoSession = niconicoSession;
            HohoemaDialogService = dialogService;
            AddTagSearchSubscriptionCommand = addTagSearchSubscriptionCommand;
            FollowButtonService = followButtonService;
            FailLoading = new ReactiveProperty<bool>(false)
                .AddTo(_CompositeDisposable);

            LoadedPage = new ReactiveProperty<int>(1)
                .AddTo(_CompositeDisposable);



            SelectedSearchSort = new ReactiveProperty<SearchSortOptionListItem>(
                VideoSearchOptionListItems.First(),
                mode: ReactivePropertyMode.DistinctUntilChanged
                );

            SelectedSearchSort
                .Subscribe(async _ =>
                {
                    var selected = SelectedSearchSort.Value;
                    if (SearchOption.Order == selected.Order
                        && SearchOption.Sort == selected.Sort
                    )
                    {
                        return;
                    }

                    SearchOption.Order = selected.Order;
                    SearchOption.Sort = selected.Sort;

                    await ResetList();
                })
                .AddTo(_CompositeDisposable);

            SelectedSearchTarget = new ReactiveProperty<SearchTarget>();
        }

        public ApplicationLayoutManager ApplicationLayoutManager { get; }
        public NGSettings NgSettings { get; }
        public Models.NiconicoSession NiconicoSession { get; }
        public SearchProvider SearchProvider { get; }
        public SubscriptionManager SubscriptionManager { get; }
        public HohoemaPlaylist HohoemaPlaylist { get; }
        public PageManager PageManager { get; }
        public Services.DialogService HohoemaDialogService { get; }
        public AddTagSearchSubscriptionCommand AddTagSearchSubscriptionCommand { get; }
        public NiconicoFollowToggleButtonService FollowButtonService { get; }

        public Models.Subscription.SubscriptionSource? SubscriptionSource => new Models.Subscription.SubscriptionSource(SearchOption.Keyword, Models.Subscription.SubscriptionSourceType.TagSearch, SearchOption.Keyword);


        static private List<SearchSortOptionListItem> _VideoSearchOptionListItems = new List<SearchSortOptionListItem>()
        {
            new SearchSortOptionListItem()
            {
                Label = Services.Helpers.SortHelper.ToCulturizedText(Sort.FirstRetrieve, Order.Descending),
                Order = Order.Descending,
                Sort = Sort.FirstRetrieve,
            },
            new SearchSortOptionListItem()
            {
                Label = Services.Helpers.SortHelper.ToCulturizedText(Sort.FirstRetrieve, Order.Ascending),
                Order = Order.Ascending,
                Sort = Sort.FirstRetrieve,
            },

            new SearchSortOptionListItem()
            {
                Label = Services.Helpers.SortHelper.ToCulturizedText(Sort.NewComment, Order.Descending),
                Order = Order.Descending,
                Sort = Sort.NewComment,
            },
            new SearchSortOptionListItem()
            {
                Label = Services.Helpers.SortHelper.ToCulturizedText(Sort.NewComment, Order.Ascending),
                Order = Order.Ascending,
                Sort = Sort.NewComment,
            },

            new SearchSortOptionListItem()
            {
                Label = Services.Helpers.SortHelper.ToCulturizedText(Sort.ViewCount, Order.Descending),
                Order = Order.Descending,
                Sort = Sort.ViewCount,
            },
            new SearchSortOptionListItem()
            {
                Label = Services.Helpers.SortHelper.ToCulturizedText(Sort.ViewCount, Order.Ascending),
                Order = Order.Ascending,
                Sort = Sort.ViewCount,
            },

            new SearchSortOptionListItem()
            {
                Label = Services.Helpers.SortHelper.ToCulturizedText(Sort.CommentCount, Order.Descending),
                Order = Order.Descending,
                Sort = Sort.CommentCount,
            },
            new SearchSortOptionListItem()
            {
                Label = Services.Helpers.SortHelper.ToCulturizedText(Sort.CommentCount, Order.Ascending),
                Order = Order.Ascending,
                Sort = Sort.CommentCount,
            },


            new SearchSortOptionListItem()
            {
                Label = Services.Helpers.SortHelper.ToCulturizedText(Sort.Length, Order.Descending),
                Order = Order.Descending,
                Sort = Sort.Length,
            },
            new SearchSortOptionListItem()
            {
                Label = Services.Helpers.SortHelper.ToCulturizedText(Sort.Length, Order.Ascending),
                Order = Order.Ascending,
                Sort = Sort.Length,
            },

            new SearchSortOptionListItem()
            {
                Label = Services.Helpers.SortHelper.ToCulturizedText(Sort.MylistCount, Order.Descending),
                Order = Order.Descending,
                Sort = Sort.MylistCount,
            },
            new SearchSortOptionListItem()
            {
                Label = Services.Helpers.SortHelper.ToCulturizedText(Sort.MylistCount, Order.Ascending),
                Order = Order.Ascending,
                Sort = Sort.MylistCount,
            },
			// V1APIだとサポートしてない
			/* 
			new SearchSortOptionListItem()
			{
				Label = "人気の高い順",
				Sort = Sort.Popurarity,
				Order = Mntone.Nico2.Order.Descending,
			},
			*/
		};

        public IReadOnlyList<SearchSortOptionListItem> VideoSearchOptionListItems => _VideoSearchOptionListItems;

        public ReactiveProperty<SearchSortOptionListItem> SelectedSearchSort { get; private set; }

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

		public ReactiveProperty<bool> FailLoading { get; private set; }

        static public TagSearchPagePayloadContent SearchOption { get; private set; }
		public ReactiveProperty<int> LoadedPage { get; private set; }


        public Database.Bookmark TagSearchBookmark { get; private set; }


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



       

		#region Commands


		private DelegateCommand _ShowSearchHistoryCommand;
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

        string ISearchWithtag.Tag => SearchOption.Keyword;

        string INiconicoObject.Id => SearchOption.Keyword;

        string INiconicoObject.Label => SearchOption.Keyword;

        #endregion

        public override Task OnNavigatedToAsync(INavigationParameters parameters)
        {
            var mode = parameters.GetNavigationMode();
            if (mode == NavigationMode.New)
            {
                Keyword = System.Net.WebUtility.UrlDecode(parameters.GetValue<string>("keyword"));

                SearchOption = new TagSearchPagePayloadContent()
                {
                    Keyword = Keyword
                };
            }

            SelectedSearchTarget.Value = SearchTarget.Tag;

            SelectedSearchSort.Value = VideoSearchOptionListItems.First(x => x.Sort == SearchOption.Sort && x.Order == SearchOption.Order);


            Database.SearchHistoryDb.Searched(SearchOption.Keyword, SearchOption.SearchTarget);

            TagSearchBookmark = Database.BookmarkDb.Get(Database.BookmarkType.SearchWithTag, SearchOption.Keyword)
                ?? new Database.Bookmark()
                {
                    BookmarkType = Database.BookmarkType.SearchWithTag,
                    Label = SearchOption.Keyword,
                    Content = SearchOption.Keyword
                };

            FollowButtonService.SetFollowTarget(this);

            RaisePropertyChanged(nameof(TagSearchBookmark));

            return base.OnNavigatedToAsync(parameters);
        }


        protected override void PostResetList()
        {
            SearchOptionText = Services.Helpers.SortHelper.ToCulturizedText(SearchOption.Sort, SearchOption.Order);

            base.PostResetList();
        }

        #region Implement HohoemaVideListViewModelBase

        protected override IIncrementalSource<VideoInfoControlViewModel> GenerateIncrementalSource()
		{
			return new VideoSearchSource(SearchOption, SearchProvider, NgSettings);
		}

		
		protected override bool CheckNeedUpdateOnNavigateTo(NavigationMode mode)
		{
			if (ItemsView?.Source == null) { return true; }

            return base.CheckNeedUpdateOnNavigateTo(mode);
        }


        #endregion


    }
}
