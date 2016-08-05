using Prism.Windows.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Windows.Navigation;
using NicoPlayerHohoema.Models;
using System.Collections.ObjectModel;
using Mntone.Nico2.Videos.Search;
using NicoPlayerHohoema.Util;
using Reactive.Bindings;
using Prism.Commands;
using Prism.Mvvm;
using System.Reactive.Linq;
using System.Diagnostics;
using Reactive.Bindings.Extensions;
using Windows.UI.Xaml.Navigation;
using System.Threading;
using Windows.UI.Xaml;

namespace NicoPlayerHohoema.ViewModels
{
	public class SearchPageViewModel : HohoemaVideoListingPageViewModelBase<VideoInfoControlViewModel>
	{

		public SearchPageViewModel(HohoemaApp hohoemaApp, PageManager pageManager, NiconicoContentFinder contentFinder, Views.Service.MylistRegistrationDialogService mylistDialogService)
			: base(hohoemaApp, pageManager, mylistDialogService, isRequireSignIn:true)
		{
			_ContentFinder = contentFinder;


			FailLoading = new ReactiveProperty<bool>(false)
				.AddTo(_CompositeDisposable);

			LoadedPage = new ReactiveProperty<int>(1)
				.AddTo(_CompositeDisposable);


			IsTagSearch = new ReactiveProperty<bool>()
				.AddTo(_CompositeDisposable);
			IsFavoriteTag = new ReactiveProperty<bool>(mode:ReactivePropertyMode.DistinctUntilChanged)
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
				if (!IsTagSearch.Value) { return; }

				_NowProcessFavorite = true;

				CanChangeFavoriteTagState.Value = false;
				if (x)
				{
					if (await FavoriteTag())
					{
						Debug.WriteLine(RequireSearchOption.Keyword + "のタグをお気に入り登録しました.");
					}
					else 
					{
						// お気に入り登録に失敗した場合は状態を差し戻し
						Debug.WriteLine(RequireSearchOption.Keyword + "のタグをお気に入り登録に失敗");
						IsFavoriteTag.Value = false;
					}
				}
				else
				{
					if (await UnfavoriteTag())
					{
						Debug.WriteLine(RequireSearchOption.Keyword + "のタグをお気に入り解除しました.");
					}
					else
					{
						// お気に入り解除に失敗した場合は状態を差し戻し
						Debug.WriteLine(RequireSearchOption.Keyword + "のタグをお気に入り解除に失敗");
						IsFavoriteTag.Value = true;
					}
				}

				CanChangeFavoriteTagState.Value = IsFavoriteTag.Value == true || HohoemaApp.FavFeedManager.CanMoreAddFavorite(FavoriteItemType.Tag);


				_NowProcessFavorite = false;
			})
			.AddTo(_CompositeDisposable);
		}


		bool _NowProcessFavorite = false;


		public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			if (e.Parameter is string)
			{
				RequireSearchOption = SearchOption.FromParameterString(e.Parameter as string);
			}

			_NowProcessFavorite = true;

			IsFavoriteTag.Value = false;
			CanChangeFavoriteTagState.Value = false;

			_NowProcessFavorite = false;

			base.OnNavigatedTo(e, viewModelState);
		}

		protected override Task ListPageNavigatedToAsync(CancellationToken cancelToken, NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			if (RequireSearchOption == null) { return Task.CompletedTask; }

			_NowProcessFavorite = true;

			IsTagSearch.Value = RequireSearchOption.SearchTarget == SearchTarget.Tag 
				&& HohoemaApp.LoginUserId != default(uint);

			if (IsTagSearch.Value)
			{
				// お気に入り登録されているかチェック
				var favManager = HohoemaApp.FavFeedManager;
				IsFavoriteTag.Value = favManager.IsFavoriteItem(FavoriteItemType.Tag, RequireSearchOption.Keyword);
				CanChangeFavoriteTagState.Value = favManager.CanMoreAddFavorite(FavoriteItemType.Tag);
			}

			_NowProcessFavorite = false;

			return Task.CompletedTask;		
		}	

		#region Implement HohoemaVideListViewModelBase

		protected override IIncrementalSource<VideoInfoControlViewModel> GenerateIncrementalSource()
		{
			return new SearchPageSource(RequireSearchOption, HohoemaApp, PageManager);
		}


		protected override void PostResetList()
		{
			var source = IncrementalLoadingItems.Source as SearchPageSource;
			var searchOption = source.SearchOption;
			var target = searchOption.SearchTarget == SearchTarget.Keyword ? "キーワード" : "タグ";
			var optionText = Util.SortMethodHelper.ToCulturizedText(searchOption.SortMethod, searchOption.SortDirection);
			UpdateTitle($"{target}検索: {searchOption.Keyword} - {optionText}");
		}

		protected override uint IncrementalLoadCount
		{
			get
			{
				return SearchPageSource.OneTimeLoadSearchItemCount / 2;
			}
		}

		protected override bool CheckNeedUpdateOnNavigateTo(NavigationMode mode)
		{
			if (mode == NavigationMode.New || mode == NavigationMode.Refresh) { return true; }

			var source = IncrementalLoadingItems.Source as SearchPageSource;

			if (RequireSearchOption != null)
			{
				return !RequireSearchOption.Equals(source.SearchOption);
			}
			else
			{
				return true;
			}
		}

		#endregion



	

		private async Task<bool> FavoriteTag()
		{
			if (!IsTagSearch.Value) { return false; }

			var favManager = HohoemaApp.FavFeedManager;
			var result = await favManager.AddFav(FavoriteItemType.Tag, RequireSearchOption.Keyword, RequireSearchOption.Keyword);

			return result == Mntone.Nico2.ContentManageResult.Success || result == Mntone.Nico2.ContentManageResult.Exist;
		}

		private async Task<bool> UnfavoriteTag()
		{
			if (!IsTagSearch.Value) { return false; }

			var favManager = HohoemaApp.FavFeedManager;
			var result = await favManager.RemoveFav(FavoriteItemType.Tag, RequireSearchOption.Keyword);

			return result == Mntone.Nico2.ContentManageResult.Success;
		}



		public ReactiveProperty<bool> IsTagSearch { get; private set; }
		public ReactiveProperty<bool> IsFavoriteTag { get; private set; }
		public ReactiveProperty<bool> CanChangeFavoriteTagState { get; private set; }


		public ReactiveCommand AddFavoriteTagCommand { get; private set; }
		public ReactiveCommand RemoveFavoriteTagCommand { get; private set; }


		public ReactiveProperty<bool> FailLoading { get; private set; }

		public SearchOption RequireSearchOption { get; private set; }
		public ReactiveProperty<int> LoadedPage { get; private set; }
		public ReactiveProperty<int> MaxPageCount { get; private set; }
	
		NiconicoContentFinder _ContentFinder;

	}

	public class SearchPageSource : IIncrementalSource<VideoInfoControlViewModel>
	{
		public const uint MaxPagenationCount = 50;
		public const int OneTimeLoadSearchItemCount = 32;

		public int MaxPageCount { get; private set; }

		HohoemaApp _HohoemaApp;
		PageManager _PageManager;
		public SearchOption SearchOption { get; private set; }

		List<ListItem> _CachedSearchListItem;


		SearchResponse _CachedSearchResponse;

		public SearchPageSource(SearchOption searchOption, HohoemaApp hohoemaApp, PageManager pageManager)
		{
			_HohoemaApp = hohoemaApp;
			_PageManager = pageManager;
			SearchOption = searchOption;

			_CachedSearchListItem = new List<ListItem>();
		}

		public async Task<int> ResetSource()
		{
			_CachedSearchResponse = await GetPageSearchRespose(1);

			MaxPageCount = Math.Min((int)Math.Floor((float)_CachedSearchResponse.count / OneTimeLoadSearchItemCount), (int)MaxPagenationCount);

			return _CachedSearchResponse.count;
		}


		private async Task<SearchResponse> GetPageSearchRespose(uint pageIndex)
		{
			if (pageIndex == 1 && _CachedSearchResponse != null)
			{
				return _CachedSearchResponse;
			}

			var contentFinder = _HohoemaApp.ContentFinder;
			SearchResponse response = null;
			switch (SearchOption.SearchTarget)
			{
				case SearchTarget.Keyword:
					response = await contentFinder.GetKeywordSearch(SearchOption.Keyword, pageIndex, SearchOption.SortMethod, SearchOption.SortDirection);
					break;
				case SearchTarget.Tag:
					response = await contentFinder.GetTagSearch(SearchOption.Keyword, pageIndex, SearchOption.SortMethod, SearchOption.SortDirection);
					break;
				default:
					break;
			}

			if (response == null || !response.IsStatusOK)
			{
				throw new Exception("page Load failed. " + pageIndex);
			}

			return response;
		}

		public async Task<IEnumerable<VideoInfoControlViewModel>> GetPagedItems(uint head, uint count)
		{
			var items = new List<VideoInfoControlViewModel>();

			// 検索は一度のレスポンスで32件を得られる
			// countが16件として来るように調節したうえで
			// １レスポンス分を２回のインクリメンタルローディングに分けて表示を行う
			var tail = head + count - 1;
			if (_CachedSearchListItem.Count < tail)
			{
				var page = (tail / OneTimeLoadSearchItemCount) + 1;

				var response = await GetPageSearchRespose(page);

				if (response.list == null)
				{
					return items;
				}

				_CachedSearchListItem.AddRange(response.list);
			}


			foreach (var item in _CachedSearchListItem.Skip((int)head).Take((int)count).ToArray())
			{
				var nicoVideo = await _HohoemaApp.MediaManager.GetNicoVideo(item.id);
				var videoInfoVM = new VideoInfoControlViewModel(
							nicoVideo
							, _PageManager
						);

				items.Add(videoInfoVM);
			}

			foreach (var item in items)
			{
				await item.LoadThumbnail().ConfigureAwait(false);
			}

			return items;
		}
	}

}
