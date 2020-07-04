﻿using System;
using System.Collections.Generic;
using System.Linq;
using Hohoema.Models;
using Hohoema.Models.Helpers;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Prism.Commands;
using Mntone.Nico2;
using System.Reactive.Linq;
using Hohoema.ViewModels.Pages;
using Hohoema.Models.Subscription;
using Hohoema.Models.Provider;
using Hohoema.Services;
using Prism.Navigation;
using System.Threading.Tasks;
using Hohoema.UseCase.Playlist;
using Hohoema.Interfaces;
using Hohoema.UseCase;
using Hohoema.ViewModels.Subscriptions;

namespace Hohoema.ViewModels
{
    public class SearchResultKeywordPageViewModel : HohoemaListingPageViewModelBase<VideoInfoControlViewModel>, INavigatedAwareAsync, IPinablePage, ITitleUpdatablePage
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

        public SearchResultKeywordPageViewModel(
            ApplicationLayoutManager applicationLayoutManager,
            NGSettings ngSettings,
            SearchProvider searchProvider,
            SubscriptionManager subscriptionManager,
            HohoemaPlaylist hohoemaPlaylist,
            PageManager pageManager,
            ViewModels.Subscriptions.AddKeywordSearchSubscriptionCommand addKeywordSearchSubscriptionCommand
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

                   await ResetList();
               })
                .AddTo(_CompositeDisposable);

            ApplicationLayoutManager = applicationLayoutManager;
            NgSettings = ngSettings;
            SearchProvider = searchProvider;
            HohoemaPlaylist = hohoemaPlaylist;
            PageManager = pageManager;
            AddKeywordSearchSubscriptionCommand = addKeywordSearchSubscriptionCommand;
            SubscriptionManager = subscriptionManager;
        }


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
        

        public Database.Bookmark KeywordSearchBookmark { get; private set; }



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

        public ApplicationLayoutManager ApplicationLayoutManager { get; }
        public NGSettings NgSettings { get; }
        public SearchProvider SearchProvider { get; }
        public SubscriptionManager SubscriptionManager1 { get; }
        public HohoemaPlaylist HohoemaPlaylist { get; }
        public PageManager PageManager { get; }
        public AddKeywordSearchSubscriptionCommand AddKeywordSearchSubscriptionCommand { get; }
        public SubscriptionManager SubscriptionManager { get; }


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

            KeywordSearchBookmark = Database.BookmarkDb.Get(Database.BookmarkType.SearchWithKeyword, SearchOption.Keyword)
                ?? new Database.Bookmark()
                {
                    BookmarkType = Database.BookmarkType.SearchWithKeyword,
                    Label = SearchOption.Keyword,
                    Content = SearchOption.Keyword
                };
            RaisePropertyChanged(nameof(KeywordSearchBookmark));

            Database.SearchHistoryDb.Searched(SearchOption.Keyword, SearchOption.SearchTarget);

            return base.OnNavigatedToAsync(parameters);
        }

        #region Implement HohoemaVideListViewModelBase

        protected override IIncrementalSource<VideoInfoControlViewModel> GenerateIncrementalSource()
		{
            return new VideoSearchSource(SearchOption, SearchProvider, NgSettings);
		}

		protected override void PostResetList()
		{
            SearchOptionText = Services.Helpers.SortHelper.ToCulturizedText(SearchOption.Sort, SearchOption.Order);
        }
		

		protected override bool CheckNeedUpdateOnNavigateTo(NavigationMode mode)
		{
			if (ItemsView?.Source == null) { return true; }

            return base.CheckNeedUpdateOnNavigateTo(mode);
        }
        #endregion

    }
}
