using Mntone.Nico2;
using NicoPlayerHohoema.Models;
using NicoPlayerHohoema.Helpers;
using NicoPlayerHohoema.Views.Service;
using Prism.Commands;
using Prism.Windows.Navigation;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Xaml.Navigation;

namespace NicoPlayerHohoema.ViewModels
{
	public class SearchResultTagPageViewModel : HohoemaVideoListingPageViewModelBase<VideoInfoControlViewModel>
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

        private string _SearchOptionText;
        public string SearchOptionText
        {
            get { return _SearchOptionText; }
            set { SetProperty(ref _SearchOptionText, value); }
        }


        public ReactiveProperty<bool> IsFavoriteTag { get; private set; }
		public ReactiveProperty<bool> CanChangeFavoriteTagState { get; private set; }


		public ReactiveCommand AddFavoriteTagCommand { get; private set; }
		public ReactiveCommand RemoveFavoriteTagCommand { get; private set; }


		public ReactiveProperty<bool> FailLoading { get; private set; }

		public TagSearchPagePayloadContent SearchOption { get; private set; }
		public ReactiveProperty<int> LoadedPage { get; private set; }


        public Database.Bookmark TagSearchBookmark { get; private set; }

		NiconicoContentProvider _ContentFinder;

        Services.HohoemaDialogService _HohoemaDialogService;

        public SearchResultTagPageViewModel(
			HohoemaApp hohoemaApp, 
			PageManager pageManager,
            Services.HohoemaDialogService dialogService
            ) 
			: base(hohoemaApp, pageManager, useDefaultPageTitle: false)
		{
			_ContentFinder = HohoemaApp.ContentProvider;
            _HohoemaDialogService = dialogService;

            FailLoading = new ReactiveProperty<bool>(false)
				.AddTo(_CompositeDisposable);

			LoadedPage = new ReactiveProperty<int>(1)
				.AddTo(_CompositeDisposable);


			IsFavoriteTag = new ReactiveProperty<bool>(mode: ReactivePropertyMode.DistinctUntilChanged)
				.AddTo(_CompositeDisposable);
			CanChangeFavoriteTagState = new ReactiveProperty<bool>()
				.AddTo(_CompositeDisposable);

			AddFavoriteTagCommand = CanChangeFavoriteTagState
				.ToReactiveCommand()
				.AddTo(_CompositeDisposable);

			RemoveFavoriteTagCommand = IsFavoriteTag
				.ToReactiveCommand()
				.AddTo(_CompositeDisposable);


			IsFavoriteTag.Subscribe(async x =>
			{
				if (_NowProcessFavorite) { return; }

				_NowProcessFavorite = true;

				CanChangeFavoriteTagState.Value = false;
				if (x)
				{
					if (await FavoriteTag())
					{
						Debug.WriteLine(SearchOption.Keyword + "のタグをお気に入り登録しました.");
					}
					else
					{
						// お気に入り登録に失敗した場合は状態を差し戻し
						Debug.WriteLine(SearchOption.Keyword + "のタグをお気に入り登録に失敗");
						IsFavoriteTag.Value = false;
					}
				}
				else
				{
					if (await UnfavoriteTag())
					{
						Debug.WriteLine(SearchOption.Keyword + "のタグをお気に入り解除しました.");
					}
					else
					{
						// お気に入り解除に失敗した場合は状態を差し戻し
						Debug.WriteLine(SearchOption.Keyword + "のタグをお気に入り解除に失敗");
						IsFavoriteTag.Value = true;
					}
				}

				CanChangeFavoriteTagState.Value = IsFavoriteTag.Value == true || HohoemaApp.FollowManager.CanMoreAddFollow(FollowItemType.Tag);


				_NowProcessFavorite = false;
			})
			.AddTo(_CompositeDisposable);

            SelectedSearchSort = new ReactiveProperty<SearchSortOptionListItem>(
                VideoSearchOptionListItems.First(),
                mode:ReactivePropertyMode.DistinctUntilChanged
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

                    pageManager.Search(SearchOption, forgetLastSearch:true);
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


        private DelegateCommand _AddFeedSourceCommand;
        public DelegateCommand AddFeedSourceCommand
        {
            get
            {
                return _AddFeedSourceCommand
                    ?? (_AddFeedSourceCommand = new DelegateCommand(async () =>
                    {
                        var targetTitle = SearchOption.Keyword;
                        var feedGroup = await HohoemaApp.ChoiceFeedGroup(targetTitle + "をフィードに追加");
                        (App.Current as App).PublishInAppNotification(
                                InAppNotificationPayload.CreateRegistrationResultNotification(
                                    feedGroup != null ? ContentManageResult.Success : ContentManageResult.Failed,
                                    "フィード",
                                    feedGroup.Label,
                                    targetTitle + "(タグ)"
                                    ));
                    }));
            }
        }
        

        #endregion

        bool _NowProcessFavorite = false;


		public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
            if (e.Parameter is string)
            {
                SearchOption = PagePayloadBase.FromParameterString<TagSearchPagePayloadContent>(e.Parameter as string);
            }

            _NowProcessFavorite = true;

			IsFavoriteTag.Value = false;
			CanChangeFavoriteTagState.Value = false;

			_NowProcessFavorite = false;

            if (SearchOption == null)
            {
                throw new Exception();
            }


            SelectedSearchSort.Value = VideoSearchOptionListItems.First(x => x.Sort == SearchOption.Sort && x.Order == SearchOption.Order);


            Models.Db.SearchHistoryDb.Searched(SearchOption.Keyword, SearchOption.SearchTarget);

            TagSearchBookmark = Database.BookmarkDb.Get(Database.BookmarkType.SearchWithTag, SearchOption.Keyword)
                ?? new Database.Bookmark()
                {
                    BookmarkType = Database.BookmarkType.SearchWithTag,
                    Label = SearchOption.Keyword,
                    Content = SearchOption.Keyword
                };
            RaisePropertyChanged(nameof(TagSearchBookmark));


            var target = "タグ";
			var optionText = Helpers.SortHelper.ToCulturizedText(SearchOption.Sort, SearchOption.Order);
            UpdateTitle($"\"{SearchOption.Keyword}\"");
            SearchOptionText = $"{target} - {optionText}";

            base.OnNavigatedTo(e, viewModelState);
		}


		protected override Task ListPageNavigatedToAsync(CancellationToken cancelToken, NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			if (SearchOption == null) { return Task.CompletedTask; }

			_NowProcessFavorite = true;
			
			// お気に入り登録されているかチェック
			var favManager = HohoemaApp.FollowManager;
			IsFavoriteTag.Value = favManager.IsFollowItem(FollowItemType.Tag, SearchOption.Keyword);
			CanChangeFavoriteTagState.Value = IsFavoriteTag.Value == true || HohoemaApp.FollowManager.CanMoreAddFollow(FollowItemType.Tag);

			_NowProcessFavorite = false;

			return base.ListPageNavigatedToAsync(cancelToken, e, viewModelState);
		}

		#region Implement HohoemaVideListViewModelBase

		protected override IIncrementalSource<VideoInfoControlViewModel> GenerateIncrementalSource()
		{
			return new VideoSearchSource(SearchOption, HohoemaApp, PageManager);
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


		private async Task<bool> FavoriteTag()
		{
			var favManager = HohoemaApp.FollowManager;
			var result = await favManager.AddFollow(FollowItemType.Tag, SearchOption.Keyword, SearchOption.Keyword);

			return result == Mntone.Nico2.ContentManageResult.Success || result == Mntone.Nico2.ContentManageResult.Exist;
		}

		private async Task<bool> UnfavoriteTag()
		{
			var favManager = HohoemaApp.FollowManager;
			var result = await favManager.RemoveFollow(FollowItemType.Tag, SearchOption.Keyword);

			return result == Mntone.Nico2.ContentManageResult.Success;
		}
	}
}
