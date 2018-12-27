using Mntone.Nico2;
using NicoPlayerHohoema.Interfaces;
using NicoPlayerHohoema.Models;
using NicoPlayerHohoema.Models.Helpers;
using NicoPlayerHohoema.Models.Provider;
using NicoPlayerHohoema.Models.Subscription;
using NicoPlayerHohoema.Services;
using NicoPlayerHohoema.Services.Page;
using Prism.Commands;
using Prism.Windows.Navigation;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Xaml.Navigation;


namespace NicoPlayerHohoema.ViewModels
{
	public class SearchResultTagPageViewModel : HohoemaListingPageViewModelBase<VideoInfoControlViewModel>, Interfaces.ISearchWithtag
    {
        public SearchResultTagPageViewModel(
           NGSettings ngSettings,
           Models.NiconicoSession niconicoSession,
           SearchProvider searchProvider,
           SubscriptionManager subscriptionManager,
           Services.HohoemaPlaylist hohoemaPlaylist,
           Services.PageManager pageManager,
           Services.DialogService dialogService,
           Commands.Subscriptions.CreateSubscriptionGroupCommand createSubscriptionGroupCommand,
           NiconicoFollowToggleButtonService followButtonService
           )
           : base(pageManager, useDefaultPageTitle: false)
        {
            SearchProvider = searchProvider;
            SubscriptionManager = subscriptionManager;
            HohoemaPlaylist = hohoemaPlaylist;
            NgSettings = ngSettings;
            NiconicoSession = niconicoSession;
            HohoemaDialogService = dialogService;
            CreateSubscriptionGroupCommand = createSubscriptionGroupCommand;
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


        public NGSettings NgSettings { get; }
        public Models.NiconicoSession NiconicoSession { get; }
        public SearchProvider SearchProvider { get; }
        public SubscriptionManager SubscriptionManager { get; }
        public Services.HohoemaPlaylist HohoemaPlaylist { get; }
        public Services.DialogService HohoemaDialogService { get; }
        public Commands.Subscriptions.CreateSubscriptionGroupCommand CreateSubscriptionGroupCommand { get; }
        public NiconicoFollowToggleButtonService FollowButtonService { get; }

        public Models.Subscription.SubscriptionSource? SubscriptionSource => new Models.Subscription.SubscriptionSource(SearchOption.Keyword, Models.Subscription.SubscriptionSourceType.TagSearch, SearchOption.Keyword);


        static private List<SearchSortOptionListItem> _VideoSearchOptionListItems = new List<SearchSortOptionListItem>()
        {
            new SearchSortOptionListItem()
            {
                Label = "投稿が新しい順",
                Order = Mntone.Nico2.Order.Descending,
                Sort = Sort.FirstRetrieve,
            },
            new SearchSortOptionListItem()
            {
                Label = "投稿が古い順",
                Order = Mntone.Nico2.Order.Ascending,
                Sort = Sort.FirstRetrieve,
            },

            new SearchSortOptionListItem()
            {
                Label = "コメントが新しい順",
                Order = Mntone.Nico2.Order.Descending,
                Sort = Sort.NewComment,
            },
            new SearchSortOptionListItem()
            {
                Label = "コメントが古い順",
                Order = Mntone.Nico2.Order.Ascending,
                Sort = Sort.NewComment,
            },

            new SearchSortOptionListItem()
            {
                Label = "再生数が多い順",
                Order = Mntone.Nico2.Order.Descending,
                Sort = Sort.ViewCount,
            },
            new SearchSortOptionListItem()
            {
                Label = "再生数が少ない順",
                Order = Mntone.Nico2.Order.Ascending,
                Sort = Sort.ViewCount,
            },

            new SearchSortOptionListItem()
            {
                Label = "コメント数が多い順",
                Order = Mntone.Nico2.Order.Descending,
                Sort = Sort.CommentCount,
            },
            new SearchSortOptionListItem()
            {
                Label = "コメント数が少ない順",
                Order = Mntone.Nico2.Order.Ascending,
                Sort = Sort.CommentCount,
            },


            new SearchSortOptionListItem()
            {
                Label = "再生時間が長い順",
                Order = Mntone.Nico2.Order.Descending,
                Sort = Sort.Length,
            },
            new SearchSortOptionListItem()
            {
                Label = "再生時間が短い順",
                Order = Mntone.Nico2.Order.Ascending,
                Sort = Sort.Length,
            },

            new SearchSortOptionListItem()
            {
                Label = "マイリスト数が多い順",
                Order = Mntone.Nico2.Order.Descending,
                Sort = Sort.MylistCount,
            },
            new SearchSortOptionListItem()
            {
                Label = "マイリスト数が少ない順",
                Order = Mntone.Nico2.Order.Ascending,
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

        private string _SearchOptionText;
        public string SearchOptionText
        {
            get { return _SearchOptionText; }
            set { SetProperty(ref _SearchOptionText, value); }
        }

		public ReactiveProperty<bool> FailLoading { get; private set; }

		public TagSearchPagePayloadContent SearchOption { get; private set; }
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
                            var payload = SearchPagePayloadContentHelper.CreateDefault(target.Value, SearchOption.Keyword);
                            PageManager.Search(payload);
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

        protected override string ResolvePageName()
        {
            return $"\"{SearchOption.Keyword}\"";
        }

        public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
            if (e.Parameter is string && e.NavigationMode == NavigationMode.New)
            {
                SearchOption = PagePayloadBase.FromParameterString<TagSearchPagePayloadContent>(e.Parameter as string);                
            }

            SelectedSearchTarget.Value = SearchOption?.SearchTarget ?? SearchTarget.Tag;

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

            

            base.OnNavigatedTo(e, viewModelState);
		}


        protected override Task ListPageNavigatedToAsync(CancellationToken cancelToken, NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			if (SearchOption == null) { return Task.CompletedTask; }

			return base.ListPageNavigatedToAsync(cancelToken, e, viewModelState);
		}

        public override void OnNavigatingFrom(NavigatingFromEventArgs e, Dictionary<string, object> viewModelState, bool suspending)
        {
            viewModelState[nameof(SearchOption)] = SearchOption.ToParameterString();

            base.OnNavigatingFrom(e, viewModelState, suspending);
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
			var source = IncrementalLoadingItems?.Source as VideoSearchSource;
			if (source == null) { return true; }

			if (SearchOption != null)
			{
				return !SearchOption.Equals(source.SearchOption);
			}
			else
			{
				return base.CheckNeedUpdateOnNavigateTo(mode);
			}
		}

		#endregion


	}
}
