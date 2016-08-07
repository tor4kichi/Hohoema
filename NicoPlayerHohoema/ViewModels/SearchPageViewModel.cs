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
using Mntone.Nico2;

namespace NicoPlayerHohoema.ViewModels
{
	public class SearchPageViewModel : HohoemaViewModelBase
	{
		public static List<SearchSortOptionListItem> SearchOptionListItems { get; private set; }

		static SearchPageViewModel()
		{
			#region SearchOptionListItems
			SearchOptionListItems = new List<SearchSortOptionListItem>()
			{
				new SearchSortOptionListItem()
				{
					Label = "投稿が新しい順",
					SortDirection = Mntone.Nico2.SortDirection.Descending,
					SortMethod = SortMethod.FirstRetrieve,
				},
				new SearchSortOptionListItem()
				{
					Label = "投稿が古い順",
					SortDirection = Mntone.Nico2.SortDirection.Ascending,
					SortMethod = SortMethod.FirstRetrieve,
				},

				new SearchSortOptionListItem()
				{
					Label = "コメントが新しい順",
					SortDirection = Mntone.Nico2.SortDirection.Descending,
					SortMethod = SortMethod.NewComment,
				},
				new SearchSortOptionListItem()
				{
					Label = "コメントが古い順",
					SortDirection = Mntone.Nico2.SortDirection.Ascending,
					SortMethod = SortMethod.NewComment,
				},

				new SearchSortOptionListItem()
				{
					Label = "再生数が多い順",
					SortDirection = Mntone.Nico2.SortDirection.Descending,
					SortMethod = SortMethod.ViewCount,
				},
				new SearchSortOptionListItem()
				{
					Label = "再生数が少ない順",
					SortDirection = Mntone.Nico2.SortDirection.Ascending,
					SortMethod = SortMethod.ViewCount,
				},

				new SearchSortOptionListItem()
				{
					Label = "コメント数が多い順",
					SortDirection = Mntone.Nico2.SortDirection.Descending,
					SortMethod = SortMethod.CommentCount,
				},
				new SearchSortOptionListItem()
				{
					Label = "コメント数が少ない順",
					SortDirection = Mntone.Nico2.SortDirection.Ascending,
					SortMethod = SortMethod.CommentCount,
				},


				new SearchSortOptionListItem()
				{
					Label = "再生時間が長い順",
					SortDirection = Mntone.Nico2.SortDirection.Descending,
					SortMethod = SortMethod.Length,
				},
				new SearchSortOptionListItem()
				{
					Label = "再生時間が短い順",
					SortDirection = Mntone.Nico2.SortDirection.Ascending,
					SortMethod = SortMethod.Length,
				},

				new SearchSortOptionListItem()
				{
					Label = "マイリスト数が多い順",
					SortDirection = Mntone.Nico2.SortDirection.Descending,
					SortMethod = SortMethod.MylistCount,
				},
				new SearchSortOptionListItem()
				{
					Label = "マイリスト数が少ない順",
					SortDirection = Mntone.Nico2.SortDirection.Ascending,
					SortMethod = SortMethod.MylistCount,
				},

				new SearchSortOptionListItem()
				{
					Label = "人気の高い順",
					SortMethod = SortMethod.Popurarity,
				},
			};
			#endregion

		}


		Views.Service.MylistRegistrationDialogService _MylistDialogService;
		public SearchOption RequireSearchOption { get; private set; }


		public ReactiveCommand DoSearchCommand { get; private set; }

		public ReactiveProperty<string> SearchText { get; private set; }
		public List<SearchTarget> TargetListItems { get; private set; }
		public ReactiveProperty<SearchTarget> SelectedTarget { get; private set; }
		public ReactiveProperty<SearchSortOptionListItem> SelectedSearchOption { get; private set; }



		public ReactiveProperty<HohoemaViewModelBase> ContentVM { get; private set; }


		public bool IsSearchKeyword
		{
			get
			{
				return RequireSearchOption?.SearchTarget == SearchTarget.Keyword;
			}
		}

		public bool IsSearchTag
		{
			get
			{
				return RequireSearchOption?.SearchTarget == SearchTarget.Tag;
			}
		}

		public bool IsSearchMylist
		{
			get
			{
				return RequireSearchOption?.SearchTarget == SearchTarget.Mylist;
			}
		}


		private void RaiseSearchTargetFlags()
		{
			OnPropertyChanged(nameof(IsSearchKeyword));
			OnPropertyChanged(nameof(IsSearchTag));
			OnPropertyChanged(nameof(IsSearchMylist));
		}

		public SearchPageViewModel(HohoemaApp hohoemaApp, PageManager pageManager, Views.Service.MylistRegistrationDialogService mylistDialogService)
			: base(hohoemaApp, pageManager, isRequireSignIn:true)
		{
			_MylistDialogService = mylistDialogService;

			ContentVM = new ReactiveProperty<HohoemaViewModelBase>();

			SearchText = new ReactiveProperty<string>("")
				.AddTo(_CompositeDisposable);

			TargetListItems = ((IEnumerable<SearchTarget>)Enum.GetValues(typeof(SearchTarget))).ToList();

			SelectedTarget = new ReactiveProperty<SearchTarget>(TargetListItems[0])
				.AddTo(_CompositeDisposable);

			SelectedTarget.Subscribe(x => 
			{
				RaiseSearchTargetFlags();
			});

			SelectedSearchOption = new ReactiveProperty<SearchSortOptionListItem>(SearchOptionListItems[0])
				.AddTo(_CompositeDisposable);


			DoSearchCommand =
				SearchText.Select(x => !String.IsNullOrEmpty(x))
				.ToReactiveCommand()
				.AddTo(_CompositeDisposable);

			DoSearchCommand.Subscribe(_ =>
			{
				if (SearchText.Value.Length == 0) { return; }

				// キーワードを検索履歴を記録
				var searchSettings = HohoemaApp.UserSettings.SearchSettings;
				searchSettings.UpdateSearchHistory(SearchText.Value);

				var searchOption = new SearchOption()
				{
					Keyword = SearchText.Value,
					SearchTarget = SelectedTarget.Value,
					SortMethod = SelectedSearchOption.Value.SortMethod,
					SortDirection = SelectedSearchOption.Value.SortDirection
				};

				// TODO: EmptySearchなページはナビゲーション後忘れさせる

				var isEmptyPage = RequireSearchOption == null;

				// 検索結果を表示
				PageManager.OpenPage(HohoemaPageType.Search,
					searchOption
					.ToParameterString()
				);

				if (isEmptyPage)
				{
					PageManager.ForgetLastPage();
				}
			})
			.AddTo(_CompositeDisposable);
		}


		public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			if (e.Parameter is string)
			{
				RequireSearchOption = SearchOption.FromParameterString(e.Parameter as string);
			}

			HohoemaViewModelBase contentVM = null;
			SearchTarget? searchTarget = RequireSearchOption?.SearchTarget;
			switch (searchTarget)
			{
				case SearchTarget.Keyword:
					contentVM = new KeywordSearchPageContentViewModel(
						HohoemaApp
						, PageManager
						, _MylistDialogService
						, RequireSearchOption
						);
					break;
				case SearchTarget.Tag:
					contentVM = new TagSearchPageContentViewModel(
						HohoemaApp
						, PageManager
						, _MylistDialogService
						, RequireSearchOption
						);
					break;
				case SearchTarget.Mylist:
					break;
				case SearchTarget.Community:
					break;
				case SearchTarget.Niconama:
					break;
				default:
					contentVM = new EmptySearchPageContentViewModel(
						HohoemaApp
						, PageManager
						);
					break;
			}

			if (contentVM == null)
			{
				throw new NotSupportedException($"not support SearchPageContent type : {RequireSearchOption.SearchTarget}");
			}

			// ContentVM側のページタイトルが後で呼び出されるように、SearchPage側を先に呼び出す
			base.OnNavigatedTo(e, viewModelState);

			contentVM.OnNavigatedTo(e, viewModelState);

			ContentVM.Value = contentVM;
		}

		public override void OnNavigatingFrom(NavigatingFromEventArgs e, Dictionary<string, object> viewModelState, bool suspending)
		{
			if (ContentVM.Value != null)
			{
				ContentVM.Value.OnNavigatingFrom(e, viewModelState, suspending);
			}

			base.OnNavigatingFrom(e, viewModelState, suspending);
		}

	}

	public class VideoSearchSource : IIncrementalSource<VideoInfoControlViewModel>
	{
		public const uint MaxPagenationCount = 50;
		public const int OneTimeLoadSearchItemCount = 32;

		public int MaxPageCount { get; private set; }

		HohoemaApp _HohoemaApp;
		PageManager _PageManager;
		public SearchOption SearchOption { get; private set; }

		List<ListItem> _CachedSearchListItem;


		SearchResponse _CachedSearchResponse;

		public VideoSearchSource(SearchOption searchOption, HohoemaApp hohoemaApp, PageManager pageManager)
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



	abstract public class SearchPageContentViewModelBase<T> : HohoemaVideoListingPageViewModelBase<T>
		where T: VideoInfoControlViewModel
	{
		public SearchPageContentViewModelBase(HohoemaApp hohoemaApp, PageManager pageManager, Views.Service.MylistRegistrationDialogService mylistDialogService)
			: base(hohoemaApp, pageManager, mylistDialogService)
		{
		}
	}
}
