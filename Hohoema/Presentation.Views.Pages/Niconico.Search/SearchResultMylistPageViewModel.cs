using Hohoema.Models.Domain;
using Hohoema.Models.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mntone.Nico2.Searches.Mylist;
using Prism.Commands;
using Mntone.Nico2;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Hohoema.Models.UseCase.PageNavigation;
using Prism.Navigation;
using System.Reactive.Linq;
using Hohoema.Models.UseCase;
using System.Threading;
using System.Runtime.CompilerServices;
using Hohoema.Models.Domain.PageNavigation;
using Hohoema.Models.Domain.Niconico.Search;
using Hohoema.Presentation.ViewModels.Niconico.Search;
using Hohoema.Models.Domain.Pins;
using Hohoema.Models.Domain.Niconico.Mylist;
using Microsoft.Toolkit.Collections;

namespace Hohoema.Presentation.ViewModels.Pages.Niconico.Search
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
            SelectedSearchSort = new ReactivePropertySlim<MylistSearchSortOptionListItem>();
            SelectedSearchTarget = new ReactiveProperty<SearchTarget>();
            ApplicationLayoutManager = applicationLayoutManager;
            SearchProvider = searchProvider;
            PageManager = pageManager;
            _searchHistoryRepository = searchHistoryRepository;
        }


        static public MylistSearchPagePayloadContent SearchOption { get; private set; }

        public static IReadOnlyList<MylistSearchSortOptionListItem> MylistSearchOptionListItems { get; private set; }

        static SearchResultMylistPageViewModel()
        {
            MylistSearchOptionListItems = new List<MylistSearchSortOptionListItem>()
            {
                new MylistSearchSortOptionListItem()
                {
                    Label = SortHelper.ToCulturizedText(Sort.MylistPopurarity, Order.Descending),
                    Sort = Sort.MylistPopurarity,
                    Order = Order.Descending,
                }
				//, new MylistSearchSortOptionListItem()
				//{
				//	Label = "人気が低い順",
				//	Sort = Sort.MylistPopurarity,
				//	Order = Order.Ascending,
				//}
				, new MylistSearchSortOptionListItem()
                {
                    Label = SortHelper.ToCulturizedText(Sort.UpdateTime, Order.Descending),
                    Sort = Sort.UpdateTime,
                    Order = Order.Descending,
                }
				//, new MylistSearchSortOptionListItem()
				//{
				//	Label = "更新が古い順",
				//	Sort = Sort.UpdateTime,
				//	Order = Order.Ascending,
				//}
				, new MylistSearchSortOptionListItem()
                {
                    Label = SortHelper.ToCulturizedText(Sort.VideoCount, Order.Descending),
                    Sort = Sort.VideoCount,
                    Order = Order.Descending,
                }
                //, new MylistSearchSortOptionListItem()
                //{
                //	Label = "動画数が少ない順",
                //	Sort = Sort.VideoCount,
                //	Order = Order.Ascending,
                //}
                , new MylistSearchSortOptionListItem()
                {
                    Label = SortHelper.ToCulturizedText(Sort.Relation, Order.Descending),
                    Sort = Sort.Relation,
                    Order = Order.Descending,
                }
				//, new MylistSearchSortOptionListItem()
				//{
				//	Label = "適合率が低い順",
				//	Sort = Sort.Relation,
				//	Order = Order.Ascending,
				//}

			};
        }

        public ReactivePropertySlim<MylistSearchSortOptionListItem> SelectedSearchSort { get; private set; }

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
                SearchOptionText = SortHelper.ToCulturizedText(SearchOption.Sort, SearchOption.Order);

                ResetList();
            })
            .AddTo(_NavigatingCompositeDisposable);

            _searchHistoryRepository.Searched(SearchOption.Keyword, SearchOption.SearchTarget);

            return base.OnNavigatedToAsync(parameters);
        }
        

        #region Implement HohoemaVideListViewModelBase


        protected override (int, IIncrementalSource<MylistPlaylist>) GenerateIncrementalSource()
		{
			return (MylistSearchSource.OneTimeLoadCount, 
                new MylistSearchSource(new MylistSearchPagePayloadContent()
                {
                    Keyword = SearchOption.Keyword,
                    Sort = SearchOption.Sort,
                    Order = SearchOption.Order
                } 
                , SearchProvider)
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

		public MylistSearchPagePayloadContent SearchOption { get; private set; }
        public SearchProvider SearchProvider { get; }

        public const int OneTimeLoadCount = 10;

        async Task<IEnumerable<MylistPlaylist>> IIncrementalSource<MylistPlaylist>.GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken ct)
        {
            var head = pageIndex * pageSize;
            var result = await SearchProvider.MylistSearchAsync(
                SearchOption.Keyword
                , (uint)head
                , (uint)pageSize
                , SearchOption.Sort
                , SearchOption.Order
            );

            ct.ThrowIfCancellationRequested();

            if (!result.IsSuccess)
            {
                return Enumerable.Empty<MylistPlaylist>();
            }

            return result.Items;
        }
    }
}
