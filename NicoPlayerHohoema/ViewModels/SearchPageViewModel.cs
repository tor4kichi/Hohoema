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

namespace NicoPlayerHohoema.ViewModels
{
	public class SearchPageViewModel : HohoemaVideoListingPageViewModelBase<VideoInfoControlViewModel>
	{
		const uint MaxPagenationCount = 50;
		const int OneTimeLoadSearchItemCount = 32;

		

		public SearchPageViewModel(HohoemaApp hohoemaApp, PageManager pageManager, NiconicoContentFinder contentFinder)
			: base(hohoemaApp, pageManager, isRequireSignIn:true)
		{
			_ContentFinder = contentFinder;


			FailLoading = new ReactiveProperty<bool>(false)
				.AddTo(_CompositeDisposable);

			LoadedPage = new ReactiveProperty<int>(1)
				.AddTo(_CompositeDisposable);
			MaxPageCount = new ReactiveProperty<int>(1)
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

			IsTagSearch.Value = RequireSearchOption.SearchTarget == SearchTarget.Tag 
				&& HohoemaApp.LoginUserId != default(uint);

			if (IsTagSearch.Value)
			{
				// お気に入り登録されているかチェック
				var favManager = HohoemaApp.FavFeedManager;
				IsFavoriteTag.Value = favManager.IsFavoriteItem(FavoriteItemType.Tag, RequireSearchOption.Keyword);
				CanChangeFavoriteTagState.Value = favManager.CanMoreAddFavorite(FavoriteItemType.Tag);
			}
			else
			{
				IsFavoriteTag.Value = false;
				CanChangeFavoriteTagState.Value = false;
			}

			_NowProcessFavorite = false;


			base.OnNavigatedTo(e, viewModelState);
		}

		public override void OnNavigatingFrom(NavigatingFromEventArgs e, Dictionary<string, object> viewModelState, bool suspending)
		{
			/*
			SearchResultItems.Clear();
			SearchResultItems = null;
			OnPropertyChanged(nameof(SearchResultItems));
			*/

			base.OnNavigatingFrom(e, viewModelState, suspending);
		}


		#region Implement HohoemaVideListViewModelBase

		protected override IIncrementalSource<VideoInfoControlViewModel> GenerateIncrementalSource()
		{
			return new SearchPageSource(this);
		}


		protected override void PostResetList()
		{
			SearchOption = RequireSearchOption;
			RequireSearchOption = null;

			var target = SearchOption.SearchTarget == SearchTarget.Keyword ? "キーワード" : "タグ";
			var optionText = Util.SortMethodHelper.ToCulturizedText(SearchOption.SortMethod, SearchOption.SortDirection);
			UpdateTitle($"{target}検索: {SearchOption.Keyword} - {optionText}");
		}

		protected override uint IncrementalLoadCount
		{
			get
			{
				return 20u;
			}
		}

		protected override bool CheckNeedUpdateOnNavigateTo(NavigationMode mode)
		{
			return !RequireSearchOption.Equals(SearchOption);
		}

		#endregion



		internal async Task<List<VideoInfoControlViewModel>> GetSearchPage(uint page)
		{
			var items = new List<VideoInfoControlViewModel>();

			var searchOption = RequireSearchOption != null ? RequireSearchOption : SearchOption;

			SearchResponse response = null;
			switch (searchOption.SearchTarget)
			{
				case SearchTarget.Keyword:
					response = await _ContentFinder.GetKeywordSearch(searchOption.Keyword, page, searchOption.SortMethod, searchOption.SortDirection);
					break;
				case SearchTarget.Tag:
					response = await _ContentFinder.GetTagSearch(searchOption.Keyword, page, searchOption.SortMethod, searchOption.SortDirection);
					break;
				default:
					break;
			}

			if (response == null || !response.IsStatusOK)
			{
				throw new Exception("page Load failed. " + page);
			}

			MaxPageCount.Value = Math.Min((int)Math.Floor((float)response.count / OneTimeLoadSearchItemCount), (int)MaxPagenationCount);

			if (response.list == null)
			{
				return items;
			}

			foreach (var item in response.list)
			{

				var nicoVideo = await HohoemaApp.MediaManager.GetNicoVideo(item.id);
				var videoInfoVM = new VideoInfoControlViewModel(
							nicoVideo
							, PageManager
						);

				items.Add(videoInfoVM);

				videoInfoVM.LoadThumbnail();

			}

			return items;
		}

	

		private async Task<bool> FavoriteTag()
		{
			if (!IsTagSearch.Value) { return false; }

			var favManager = HohoemaApp.FavFeedManager;
			var result = await favManager.AddFav(FavoriteItemType.Tag, SearchOption.Keyword);

			return result == Mntone.Nico2.ContentManageResult.Success || result == Mntone.Nico2.ContentManageResult.Exist;
		}

		private async Task<bool> UnfavoriteTag()
		{
			if (!IsTagSearch.Value) { return false; }

			var favManager = HohoemaApp.FavFeedManager;
			var result = await favManager.RemoveFav(FavoriteItemType.Tag, SearchOption.Keyword);

			return result == Mntone.Nico2.ContentManageResult.Success;
		}



		public ReactiveProperty<bool> IsTagSearch { get; private set; }
		public ReactiveProperty<bool> IsFavoriteTag { get; private set; }
		public ReactiveProperty<bool> CanChangeFavoriteTagState { get; private set; }


		public ReactiveCommand AddFavoriteTagCommand { get; private set; }
		public ReactiveCommand RemoveFavoriteTagCommand { get; private set; }


		public ReactiveProperty<bool> FailLoading { get; private set; }

		public SearchOption RequireSearchOption { get; private set; }
		public SearchOption SearchOption { get; private set; }
		public ReactiveProperty<int> LoadedPage { get; private set; }
		public ReactiveProperty<int> MaxPageCount { get; private set; }

		public ReactiveProperty<bool> NowPageLoading { get; private set; }
	
		NiconicoContentFinder _ContentFinder;

	}

	public class SearchPageSource : IIncrementalSource<VideoInfoControlViewModel>
	{
		public SearchPageSource(SearchPageViewModel parentVM)
		{
			_ParentVM = parentVM;
		}


		public async Task<IEnumerable<VideoInfoControlViewModel>> GetPagedItems(uint position, uint pageSize)
		{
			var pageIndex = position / pageSize;

			Debug.WriteLine(pageIndex);

			return await _ParentVM.GetSearchPage(pageIndex + 1);
		}



		SearchPageViewModel _ParentVM;
	}

}
