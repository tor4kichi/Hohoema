using Mntone.Nico2.Mylist;
using NicoPlayerHohoema.Models;
using NicoPlayerHohoema.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using NicoPlayerHohoema.Views.Service;
using Windows.UI.Xaml.Navigation;
using Mntone.Nico2.Searches.Mylist;
using Prism.Commands;
using Mntone.Nico2;
using Prism.Windows.Navigation;
using System.Collections.Async;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace NicoPlayerHohoema.ViewModels
{
	public class SearchResultMylistPageViewModel : HohoemaListingPageViewModelBase<IPlayableList>
	{
		public MylistSearchPagePayloadContent SearchOption { get; private set; }

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
                            var payload = SearchPagePayloadContentHelper.CreateDefault(target.Value, SearchOption.Keyword);
                            PageManager.Search(payload, true);
                        }
                    }));
            }
        }

        public SearchResultMylistPageViewModel(
			HohoemaApp hohoemaApp
			, PageManager pageManager
			) 
			: base(hohoemaApp, pageManager, useDefaultPageTitle: false)
		{
            SelectedSearchSort = new ReactivePropertySlim<SearchSortOptionListItem>();
            SelectedSearchTarget = new ReactiveProperty<SearchTarget>();
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

        #endregion

        protected override string ResolvePageName()
        {
            return SearchOption.Keyword;
        }

        public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
            if (e.Parameter is string)
            {
                SearchOption = PagePayloadBase.FromParameterString<MylistSearchPagePayloadContent>(e.Parameter as string);
            }

            SelectedSearchTarget.Value = SearchOption?.SearchTarget ?? SearchTarget.Mylist;

            if (SearchOption == null)
            {
                throw new Exception();
            }

            SelectedSearchSort.Value = MylistSearchOptionListItems.FirstOrDefault(x => x.Order == SearchOption.Order && x.Sort == SearchOption.Sort);

            SelectedSearchSort.Subscribe(async opt =>
            {
                SearchOption.Order = opt.Order;
                SearchOption.Sort = opt.Sort;
                SearchOptionText = Helpers.SortHelper.ToCulturizedText(SearchOption.Sort, SearchOption.Order);

                await ResetList();
            })
            .AddTo(_NavigatingCompositeDisposable);

            Database.SearchHistoryDb.Searched(SearchOption.Keyword, SearchOption.SearchTarget);

            

            base.OnNavigatedTo(e, viewModelState);
		}

		#region Implement HohoemaVideListViewModelBase


		protected override IIncrementalSource<IPlayableList> GenerateIncrementalSource()
		{
			return new MylistSearchSource(SearchOption, HohoemaApp, PageManager);
		}

		protected override bool CheckNeedUpdateOnNavigateTo(NavigationMode mode)
		{
			var source = IncrementalLoadingItems?.Source as MylistSearchSource;
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

	public class MylistSearchSource : IIncrementalSource<IPlayableList>
	{
		public int MaxPageCount { get; private set; }

		HohoemaApp _HohoemaApp;
		PageManager _PageManager;
		public MylistSearchPagePayloadContent SearchOption { get; private set; }
		
		private MylistSearchResponse _MylistGroupResponse;



		public MylistSearchSource(MylistSearchPagePayloadContent searchOption, HohoemaApp hohoemaApp, PageManager pageManager)
		{
			_HohoemaApp = hohoemaApp;
			_PageManager = pageManager;
			SearchOption = searchOption;
		}





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
			_MylistGroupResponse = await _HohoemaApp.NiconicoContext.Search.MylistSearchAsync(
				SearchOption.Keyword,
				0,
				2,
				SearchOption.Sort, 
				SearchOption.Order
				);

			return (int)_MylistGroupResponse.GetTotalCount();
		}


		

		public async Task<IAsyncEnumerable<IPlayableList>> GetPagedItems(int head, int count)
		{
			var response = await _HohoemaApp.NiconicoContext.Search.MylistSearchAsync(
				SearchOption.Keyword
				, (uint)head
				, (uint)count
				, SearchOption.Sort
				, SearchOption.Order
			);

            return response.MylistGroupItems?
                .Select(item => new OtherOwneredMylist(item) as IPlayableList)
                .ToAsyncEnumerable()
            ?? AsyncEnumerable.Empty<IPlayableList>();
        }
	}
}
