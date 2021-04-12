using Hohoema.Models.Domain;
using Hohoema.Models.Domain.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mntone.Nico2.Searches.Mylist;
using Prism.Commands;
using Mntone.Nico2;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Hohoema.Presentation.Services.Page;
using Prism.Navigation;
using System.Reactive.Linq;
using Hohoema.Models.UseCase;
using System.Threading;
using System.Runtime.CompilerServices;
using Hohoema.Models.Domain.PageNavigation;
using Hohoema.Models.Domain.Niconico.UserFeature.Mylist;
using Hohoema.Models.Domain.Niconico.Search;

namespace Hohoema.Presentation.ViewModels.Pages.SearchPages
{
    public class SearchResultMylistPageViewModel : HohoemaListingPageViewModelBase<MylistPlaylist>, INavigatedAwareAsync, IPinablePage, ITitleUpdatablePage
    {
        HohoemaPin IPinablePage.GetPin()
        {
            return new HohoemaPin()
            {
                Label = SearchOption.Keyword,
                PageType = HohoemaPageType.SearchResultMylist,
                Parameter = $"keyword={System.Net.WebUtility.UrlEncode(SearchOption.Keyword)}&target={SearchOption.SearchTarget}"
            };
        }

        IObservable<string> ITitleUpdatablePage.GetTitleObservable()
        {
            return this.ObserveProperty(x => x.Keyword);
        }

        public SearchResultMylistPageViewModel(
            ApplicationLayoutManager applicationLayoutManager,
            SearchProvider searchProvider,
            PageManager pageManager,
            SearchHistoryRepository searchHistoryRepository
            )
        {
            SelectedSearchSort = new ReactivePropertySlim<SearchSortOptionListItem>();
            SelectedSearchTarget = new ReactiveProperty<SearchTarget>();
            ApplicationLayoutManager = applicationLayoutManager;
            SearchProvider = searchProvider;
            PageManager = pageManager;
            _searchHistoryRepository = searchHistoryRepository;
        }


        static public MylistSearchPagePayloadContent SearchOption { get; private set; }

        public static IReadOnlyList<SearchSortOptionListItem> MylistSearchOptionListItems { get; private set; }

        static SearchResultMylistPageViewModel()
        {
            MylistSearchOptionListItems = new List<SearchSortOptionListItem>()
            {
                new SearchSortOptionListItem()
                {
                    Label = Services.Helpers.SortHelper.ToCulturizedText(Sort.MylistPopurarity, Order.Descending),
                    Sort = Sort.MylistPopurarity,
                    Order = Order.Descending,
                }
				//, new SearchSortOptionListItem()
				//{
				//	Label = "人気が低い順",
				//	Sort = Sort.MylistPopurarity,
				//	Order = Order.Ascending,
				//}
				, new SearchSortOptionListItem()
                {
                    Label = Services.Helpers.SortHelper.ToCulturizedText(Sort.UpdateTime, Order.Descending),
                    Sort = Sort.UpdateTime,
                    Order = Order.Descending,
                }
				//, new SearchSortOptionListItem()
				//{
				//	Label = "更新が古い順",
				//	Sort = Sort.UpdateTime,
				//	Order = Order.Ascending,
				//}
				, new SearchSortOptionListItem()
                {
                    Label = Services.Helpers.SortHelper.ToCulturizedText(Sort.VideoCount, Order.Descending),
                    Sort = Sort.VideoCount,
                    Order = Order.Descending,
                }
                //, new SearchSortOptionListItem()
                //{
                //	Label = "動画数が少ない順",
                //	Sort = Sort.VideoCount,
                //	Order = Order.Ascending,
                //}
                , new SearchSortOptionListItem()
                {
                    Label = Services.Helpers.SortHelper.ToCulturizedText(Sort.Relation, Order.Descending),
                    Sort = Sort.Relation,
                    Order = Order.Descending,
                }
				//, new SearchSortOptionListItem()
				//{
				//	Label = "適合率が低い順",
				//	Sort = Sort.Relation,
				//	Order = Order.Ascending,
				//}

			};
        }

        public ReactivePropertySlim<SearchSortOptionListItem> SelectedSearchSort { get; private set; }

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

        public ApplicationLayoutManager ApplicationLayoutManager { get; }
        public SearchProvider SearchProvider { get; }
        public PageManager PageManager { get; }

        #endregion

        public override Task OnNavigatedToAsync(INavigationParameters parameters)
        {
            var mode = parameters.GetNavigationMode();
            if (mode == NavigationMode.New)
            {
                Keyword = System.Net.WebUtility.UrlDecode(parameters.GetValue<string>("keyword"));

                SearchOption = new MylistSearchPagePayloadContent()
                {
                    Keyword = Keyword
                };
            }


            SelectedSearchTarget.Value = SearchTarget.Mylist;

            SelectedSearchSort.Value = MylistSearchOptionListItems.FirstOrDefault(x => x.Order == SearchOption.Order && x.Sort == SearchOption.Sort);

            SelectedSearchSort.Subscribe(async opt =>
            {
                SearchOption.Order = opt.Order;
                SearchOption.Sort = opt.Sort;
                SearchOptionText = Services.Helpers.SortHelper.ToCulturizedText(SearchOption.Sort, SearchOption.Order);

                await ResetList();
            })
            .AddTo(_NavigatingCompositeDisposable);

            _searchHistoryRepository.Searched(SearchOption.Keyword, SearchOption.SearchTarget);

            return base.OnNavigatedToAsync(parameters);
        }
        

        #region Implement HohoemaVideListViewModelBase


        protected override IIncrementalSource<MylistPlaylist> GenerateIncrementalSource()
		{
			return new MylistSearchSource(new MylistSearchPagePayloadContent()
            {
                Keyword = SearchOption.Keyword,
                Sort = SearchOption.Sort,
                Order = SearchOption.Order
            } 
            , SearchProvider
            );
		}

		protected override bool CheckNeedUpdateOnNavigateTo(NavigationMode mode)
		{
			if (ItemsView?.Source == null) { return true; }

            return base.CheckNeedUpdateOnNavigateTo(mode);
        }

        #endregion
    }

	public class MylistSearchSource : IIncrementalSource<MylistPlaylist>
	{
        public MylistSearchSource(
            MylistSearchPagePayloadContent searchOption,
            SearchProvider searchProvider
            )
        {
            SearchOption = searchOption;
            SearchProvider = searchProvider;
        }


        public int MaxPageCount { get; private set; }

		public MylistSearchPagePayloadContent SearchOption { get; private set; }
        public SearchProvider SearchProvider { get; }

        private MylistSearchResponse _MylistGroupResponse;






		public uint OneTimeLoadCount
		{
			get
			{
				return 10;
			}
		}


		public async ValueTask<int> ResetSource()
		{
			// Note: 件数が1だとJsonのParseがエラーになる
			var res = await SearchProvider.MylistSearchAsync(
				SearchOption.Keyword,
				0,
				2,
				SearchOption.Sort, 
				SearchOption.Order
				);

			return res.TotalCount;
		}


		

		public async IAsyncEnumerable<MylistPlaylist> GetPagedItems(int head, int count, [EnumeratorCancellation] CancellationToken ct = default)
		{
			var result = await SearchProvider.MylistSearchAsync(
				SearchOption.Keyword
				, (uint)head
				, (uint)count
				, SearchOption.Sort
				, SearchOption.Order
			);

            ct.ThrowIfCancellationRequested();

            if (result.IsSuccess)
            {
                foreach (var item in result.Items)
                {
                    yield return item;

                    ct.ThrowIfCancellationRequested();
                }
            }
        }
	}
}
