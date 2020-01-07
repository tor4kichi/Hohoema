using NicoPlayerHohoema.Models;
using NicoPlayerHohoema.Models.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mntone.Nico2.Searches.Mylist;
using Prism.Commands;
using Mntone.Nico2;
using System.Collections.Async;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using NicoPlayerHohoema.Services.Page;
using NicoPlayerHohoema.Models.Provider;
using NicoPlayerHohoema.Services;
using Prism.Navigation;
using NicoPlayerHohoema.Interfaces;
using NicoPlayerHohoema.Repository.Playlist;

namespace NicoPlayerHohoema.ViewModels
{
    public class SearchResultMylistPageViewModel : HohoemaListingPageViewModelBase<MylistPlaylist>, INavigatedAwareAsync
    {
        public SearchResultMylistPageViewModel(
            SearchProvider searchProvider,
            Services.PageManager pageManager
            )
        {
            SelectedSearchSort = new ReactivePropertySlim<SearchSortOptionListItem>();
            SelectedSearchTarget = new ReactiveProperty<SearchTarget>();
            SearchProvider = searchProvider;
            PageManager = pageManager;
        }


        static public MylistSearchPagePayloadContent SearchOption { get; private set; }

        public static IReadOnlyList<SearchSortOptionListItem> MylistSearchOptionListItems { get; private set; }

        static SearchResultMylistPageViewModel()
        {
            MylistSearchOptionListItems = new List<SearchSortOptionListItem>()
            {
                new SearchSortOptionListItem()
                {
                    Label = "人気が高い順",
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
                    Label = "更新が新しい順",
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
                    Label = "動画数が多い順",
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
                    Label = "適合率が高い順",
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

        public SearchProvider SearchProvider { get; }
        public PageManager PageManager { get; }

        #endregion

        public override Task OnNavigatedToAsync(INavigationParameters parameters)
        {
            var mode = parameters.GetNavigationMode();
            if (mode == NavigationMode.New)
            {
                SearchOption = new MylistSearchPagePayloadContent()
                {
                    Keyword = System.Net.WebUtility.UrlDecode(parameters.GetValue<string>("keyword"))
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

            Database.SearchHistoryDb.Searched(SearchOption.Keyword, SearchOption.SearchTarget);

            PageManager.PageTitle = $"\"{SearchOption.Keyword}\"";

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
			if (ItemsView.Source == null) { return true; }

            return base.CheckNeedUpdateOnNavigateTo(mode);
        }

        protected override bool TryGetHohoemaPin(out HohoemaPin pin)
        {
            pin = new HohoemaPin()
            {
                Label = SearchOption.Keyword,
                PageType = HohoemaPageType.SearchResultMylist,
                Parameter = $"keyword={System.Net.WebUtility.UrlEncode(SearchOption.Keyword)}&target={SearchOption.SearchTarget}"
            };

            return true;
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


		public async Task<int> ResetSource()
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


		

		public async Task<IAsyncEnumerable<MylistPlaylist>> GetPagedItems(int head, int count)
		{
			var result = await SearchProvider.MylistSearchAsync(
				SearchOption.Keyword
				, (uint)head
				, (uint)count
				, SearchOption.Sort
				, SearchOption.Order
			);

            return result.IsSuccess ? result.Items.ToAsyncEnumerable() : AsyncEnumerable.Empty<MylistPlaylist>();
        }
	}
}
