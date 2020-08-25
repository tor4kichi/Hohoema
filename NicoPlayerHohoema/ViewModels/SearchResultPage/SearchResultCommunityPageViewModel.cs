using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NicoPlayerHohoema.Models;
using NicoPlayerHohoema.Services;
using NicoPlayerHohoema.Models.Helpers;
using System.Windows.Input;
using Mntone.Nico2.Searches.Community;
using Mntone.Nico2;
using Prism.Commands;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Reactive.Linq;
using NicoPlayerHohoema.Services.Page;
using NicoPlayerHohoema.Models.Provider;
using Prism.Navigation;
using NicoPlayerHohoema.Interfaces;
using NicoPlayerHohoema.UseCase;
using I18NPortable;
using System.Runtime.CompilerServices;
using System.Threading;

namespace NicoPlayerHohoema.ViewModels
{

	// Note: Communityの検索はページベースで行います。
	// また、ログインが必要です。

	public class SearchResultCommunityPageViewModel : HohoemaListingPageViewModelBase<CommunityInfoControlViewModel>, INavigatedAwareAsync, IPinablePage, ITitleUpdatablePage
    {
        HohoemaPin IPinablePage.GetPin()
        {
            return new HohoemaPin()
            {
                Label = SearchOption.Keyword,
                PageType = HohoemaPageType.SearchResultCommunity,
                Parameter = $"keyword={System.Net.WebUtility.UrlEncode(SearchOption.Keyword)}&target={SearchOption.SearchTarget}"
            };
        }


        IObservable<string> ITitleUpdatablePage.GetTitleObservable()
        {
            return this.ObserveProperty(x => x.Keyword);
        }

        public SearchResultCommunityPageViewModel(
            ApplicationLayoutManager applicationLayoutManager,
            PageManager pageManager, 
            SearchProvider searchProvider,
            Services.NiconicoLoginService niconicoLoginService
            )
        {
            ApplicationLayoutManager = applicationLayoutManager;
            PageManager = pageManager;
            SearchProvider = searchProvider;
            NiconicoLoginService = niconicoLoginService;
            SelectedSearchSort = new ReactivePropertySlim<CommunitySearchSortOptionListItem>();
            SelectedSearchMode = new ReactivePropertySlim<CommynitySearchModeOptionListItem>();

            SelectedSearchTarget = new ReactiveProperty<SearchTarget>();
        }

        public ApplicationLayoutManager ApplicationLayoutManager { get; }
        public PageManager PageManager { get; }

        public SearchProvider SearchProvider { get; }
        public NiconicoLoginService NiconicoLoginService { get; }

        static public CommunitySearchPagePayloadContent SearchOption { get; private set; }

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

        public static List<SearchTarget> SearchTargets { get; } = Enum.GetValues(typeof(SearchTarget)).Cast<SearchTarget>().ToList();

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


        public class CommunitySearchSortOptionListItem
        {
            public string Label { get; set; }
            public CommunitySearchSort Sort { get; set; }
            public Order Order { get; set; }
        }

        public class CommynitySearchModeOptionListItem
        {
            public string Label { get; set; }
            public CommunitySearchMode Mode { get; set; }
        }

        public static IReadOnlyList<CommunitySearchSortOptionListItem> CommunitySearchSortOptionListItems { get; private set; }
        public static IReadOnlyList<CommynitySearchModeOptionListItem> CommunitySearchModeOptionListItems { get; private set; }

        static SearchResultCommunityPageViewModel()
        {
            var sortList = new[]
            {
                CommunitySearchSort.CreatedAt,
                CommunitySearchSort.UpdateAt,
                CommunitySearchSort.CommunityLevel,
                CommunitySearchSort.VideoCount,
                CommunitySearchSort.MemberCount
            };

            CommunitySearchSortOptionListItems = sortList.SelectMany(x =>
            {
                return new List<CommunitySearchSortOptionListItem>()
                {
                    new CommunitySearchSortOptionListItem()
                    {
                        Sort = x,
                        Order = Order.Descending,
                    },
                    new CommunitySearchSortOptionListItem()
                    {
                        Sort = x,
                        Order = Order.Ascending,
                    },
                };
            })
            .ToList();

            foreach (var item in CommunitySearchSortOptionListItems)
            {
                item.Label = Services.Helpers.SortHelper.ToCulturizedText(item.Sort, item.Order);
            }


            CommunitySearchModeOptionListItems = new List<CommynitySearchModeOptionListItem>()
            {
                new CommynitySearchModeOptionListItem()
                {
                    Label = "SearchWithKeyword".Translate(),
                    Mode = CommunitySearchMode.Keyword
                },
                new CommynitySearchModeOptionListItem()
                {
                    Label = "CommunitySearchWithTag".Translate(),
                    Mode = CommunitySearchMode.Tag
                },
            };
        }

        public ReactivePropertySlim<CommunitySearchSortOptionListItem> SelectedSearchSort { get; private set; }
        public ReactivePropertySlim<CommynitySearchModeOptionListItem> SelectedSearchMode { get; private set; }




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

        #endregion

        public override Task OnNavigatedToAsync(INavigationParameters parameters)
        {
            var mode = parameters.GetNavigationMode();
            if (mode == NavigationMode.New)
            {
                Keyword = System.Net.WebUtility.UrlDecode(parameters.GetValue<string>("keyword"));

                SearchOption = new CommunitySearchPagePayloadContent()
                {
                    Keyword = Keyword
                };
            }

            SelectedSearchTarget.Value = SearchOption?.SearchTarget ?? SearchTarget.Community;

            if (SearchOption == null)
            {
                throw new Exception("コミュニティ検索");
            }

            SelectedSearchSort.Value = CommunitySearchSortOptionListItems.FirstOrDefault(x => x.Order == SearchOption.Order && x.Sort == SearchOption.Sort);
            SelectedSearchMode.Value = CommunitySearchModeOptionListItems.FirstOrDefault(x => x.Mode == SearchOption.Mode);


            new[] {
                SelectedSearchSort.ToUnit(),
                SelectedSearchMode.ToUnit()
                }
            .CombineLatest()
            .Subscribe(async _ =>
            {
                SearchOption.Sort = SelectedSearchSort.Value.Sort;
                SearchOption.Order = SelectedSearchSort.Value.Order;
                SearchOption.Mode = SelectedSearchMode.Value.Mode;

                await ResetList();
            })
            .AddTo(_NavigatingCompositeDisposable);

            Database.SearchHistoryDb.Searched(SearchOption.Keyword, SearchOption.SearchTarget);

            return base.OnNavigatedToAsync(parameters);
        }

        protected override IIncrementalSource<CommunityInfoControlViewModel> GenerateIncrementalSource()
		{
			return new CommunitySearchSource(
                new CommunitySearchPagePayloadContent()
                {
                    Keyword = SearchOption.Keyword,
                    Mode = SearchOption.Mode,
                    Order = SearchOption.Order,
                    Sort = SearchOption.Sort
                },
                SearchProvider
            );
		}

        protected override void PostResetList()
        {
            RefreshSearchOptionText();

            base.PostResetList();
        }

        private void RefreshSearchOptionText()
        {
            var optionText = Services.Helpers.SortHelper.ToCulturizedText(SearchOption.Sort, SearchOption.Order);
            var mode = (SearchOption.Mode == CommunitySearchMode.Keyword ? "Keyword" : "Tag").Translate();

            SearchOptionText = $"{optionText}({mode})";
        }
    }

	public class CommunitySearchSource : IIncrementalSource<CommunityInfoControlViewModel>
	{
        public CommunitySearchSource(
            CommunitySearchPagePayloadContent searchOption,
            SearchProvider searchProvider
            )
        {
            SearchKeyword = searchOption.Keyword;
            Mode = searchOption.Mode;
            Sort = searchOption.Sort;
            Order = searchOption.Order;
            SearchProvider = searchProvider;
        }

        public uint OneTimeLoadCount => 10;

		public uint TotalCount { get; private set; }
		public CommunitySearchResponse FirstResponse { get; private set; }

		public string SearchKeyword { get; private set; }
		public CommunitySearchMode Mode { get; private set; }
		public CommunitySearchSort Sort { get; private set; }
		public Order Order { get; private set; }
        public SearchProvider SearchProvider { get; }

        public async Task<int> ResetSource()
		{
			try
			{
				FirstResponse = await SearchProvider.SearchCommunity(
					SearchKeyword
					, 1
					, Sort
					, Order
					, Mode
					);

				if (FirstResponse != null)
				{
					TotalCount = FirstResponse.TotalCount;
				}
			}
			catch { }
			

			return (int)TotalCount;
		}

		public async IAsyncEnumerable<CommunityInfoControlViewModel> GetPagedItems(int head, int count, [EnumeratorCancellation] CancellationToken ct = default)
		{
			CommunitySearchResponse res = head == 0 ? FirstResponse : null;

			if (res == null)
			{
				var page = (uint)((head + count) / OneTimeLoadCount);
				res = await SearchProvider.SearchCommunity(
					SearchKeyword
					, page
					, Sort
					, Order
					, Mode
					);
			}

            ct.ThrowIfCancellationRequested();

			if (res == null)
			{
                yield break;
			}

			if (false == res.IsStatusOK)
			{
                yield break;
            }

            foreach (var item in res.Communities)
            {
                yield return new CommunityInfoControlViewModel(item);
                ct.ThrowIfCancellationRequested();
            }
		}

	}

	public class CommunityInfoControlViewModel : HohoemaListingPageItemBase, Interfaces.ICommunity
    {
		public string Name { get; private set; }
		public string ShortDescription { get; private set; }
		public string UpdateDate { get; private set; }
		public string IconUrl { get; private set; }
		public uint Level { get; private set; }
		public uint MemberCount { get; private set; }
		public uint VideoCount { get; private set; }

		public string CommunityId { get; private set; }

        public string Id => CommunityId;

        public CommunityInfoControlViewModel(Mntone.Nico2.Searches.Community.NicoCommynity commu)
		{
			CommunityId = commu.Id;
            Name = commu.Name;
            ShortDescription = commu.ShortDescription;
            UpdateDate = commu.DateTime;
            IconUrl = commu.IconUrl.AbsoluteUri;

            Level = commu.Level;
			MemberCount = commu.MemberCount;
			VideoCount = commu.VideoCount;

            Label = commu.Name;
            Description = commu.ShortDescription;
            AddImageUrl(commu.IconUrl.OriginalString);
        }

        

	}
}
