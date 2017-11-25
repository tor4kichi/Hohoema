using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NicoPlayerHohoema.Models;
using NicoPlayerHohoema.Helpers;
using NicoPlayerHohoema.Views.Service;
using Windows.UI.Xaml.Navigation;
using Prism.Windows.Navigation;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Prism.Commands;
using Mntone.Nico2;
using System.Reactive.Linq;

namespace NicoPlayerHohoema.ViewModels
{
	public class SearchResultKeywordPageViewModel : HohoemaVideoListingPageViewModelBase<VideoInfoControlViewModel>
	{

        private static List<SearchSortOptionListItem> _VideoSearchOptionListItems = new List<SearchSortOptionListItem>()
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


        public ReactiveProperty<bool> FailLoading { get; private set; }

		public KeywordSearchPagePayloadContent SearchOption { get; private set; }
		public ReactiveProperty<int> LoadedPage { get; private set; }

        private string _SearchOptionText;
        public string SearchOptionText
        {
            get { return _SearchOptionText; }
            set { SetProperty(ref _SearchOptionText, value); }
        }
        

        NiconicoContentProvider _ContentFinder;

        public Database.Bookmark KeywordSearchBookmark { get; private set; }


		public SearchResultKeywordPageViewModel(
			HohoemaApp hohoemaApp, 
			PageManager pageManager
			) 
			: base(hohoemaApp, pageManager, useDefaultPageTitle: false)
		{
			_ContentFinder = HohoemaApp.ContentProvider;

			FailLoading = new ReactiveProperty<bool>(false)
				.AddTo(_CompositeDisposable);

			LoadedPage = new ReactiveProperty<int>(1)
				.AddTo(_CompositeDisposable);

            SelectedSearchSort = new ReactiveProperty<SearchSortOptionListItem>(
                VideoSearchOptionListItems.First(),
                mode: ReactivePropertyMode.DistinctUntilChanged
                );

            SelectedSearchSort
               .Subscribe(_ =>
               {
                   var selected = SelectedSearchSort.Value;
                   if (SearchOption.Order == selected.Order
                       && SearchOption.Sort == selected.Sort
                   )
                   {
                       return;
                   }

                   SearchOption.Sort = SelectedSearchSort.Value.Sort;
                   SearchOption.Order = SelectedSearchSort.Value.Order;

                   pageManager.Search(SearchOption, forgetLastSearch: true);
               })
                .AddTo(_CompositeDisposable);
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



        public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
            if (e.Parameter is string)
            {
                SearchOption = PagePayloadBase.FromParameterString<KeywordSearchPagePayloadContent>(e.Parameter as string);
            }

            if (SearchOption == null)
            {
                throw new Exception("");
            }


            SelectedSearchSort.Value = VideoSearchOptionListItems.First(x => x.Sort == SearchOption.Sort && x.Order == SearchOption.Order);

            Models.Db.SearchHistoryDb.Searched(SearchOption.Keyword, SearchOption.SearchTarget);

            KeywordSearchBookmark = Database.BookmarkDb.Get(Database.BookmarkType.SearchWithKeyword, SearchOption.Keyword)
                ?? new Database.Bookmark()
                {
                    BookmarkType = Database.BookmarkType.SearchWithKeyword,
                    Label = SearchOption.Keyword,
                    Content = SearchOption.Keyword
                };
            RaisePropertyChanged(nameof(KeywordSearchBookmark));

            var target = "キーワード";
			var optionText = Helpers.SortHelper.ToCulturizedText(SearchOption.Sort, SearchOption.Order);
            UpdateTitle($"\"{SearchOption.Keyword}\"");
            SearchOptionText = $"{target} - {optionText}";

            base.OnNavigatedTo(e, viewModelState);
		}

		#region Implement HohoemaVideListViewModelBase



		protected override IIncrementalSource<VideoInfoControlViewModel> GenerateIncrementalSource()
		{
            return new VideoSearchSource(SearchOption, HohoemaApp, PageManager);
		}

		protected override void PostResetList()
		{
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
