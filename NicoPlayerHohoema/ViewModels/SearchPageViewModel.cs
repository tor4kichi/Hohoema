using Prism.Windows.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Windows.Navigation;
using NicoPlayerHohoema.Models;
using System.Collections.ObjectModel;
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
using Mntone.Nico2.Searches.Video;
using NicoPlayerHohoema.Models.Db;

namespace NicoPlayerHohoema.ViewModels
{
	public class SearchPageViewModel : HohoemaViewModelBase
	{
		public static List<SearchSortOptionListItem> VideoSearchOptionListItems { get; private set; }
		public static List<SearchSortOptionListItem> MylistSearchOptionListItems { get; private set; }

		static SearchPageViewModel()
		{
			#region SearchOptionListItems
			VideoSearchOptionListItems = new List<SearchSortOptionListItem>()
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

				new SearchSortOptionListItem()
				{
					Label = "人気の高い順",
					Sort = Sort.Popurarity,
					Order = Mntone.Nico2.Order.Descending,
				},
			};
			#endregion


			#region マイリスト検索時のオプション

			MylistSearchOptionListItems = new List<SearchSortOptionListItem>()
			{
				new SearchSortOptionListItem()
				{
					Label = "人気が高い順",
					Sort = Sort.MylistPopurarity,
					Order = Order.Descending,
				}
				,
				new SearchSortOptionListItem()
				{
					Label = "人気が低い順",
					Sort = Sort.MylistPopurarity,
					Order = Order.Ascending,
				}
				,
				new SearchSortOptionListItem()
				{
					Label = "更新が新しい順",
					Sort = Sort.UpdateTime,
					Order = Order.Descending,
				}
				,
				new SearchSortOptionListItem()
				{
					Label = "更新が古い順",
					Sort = Sort.UpdateTime,
					Order = Order.Ascending,
				}
				,
				new SearchSortOptionListItem()
				{
					Label = "動画数が多い順",
					Sort = Sort.VideoCount,
					Order = Order.Descending,
				}
				,
				new SearchSortOptionListItem()
				{
					Label = "動画数が少ない順",
					Sort = Sort.VideoCount,
					Order = Order.Ascending,
				}
				,
				new SearchSortOptionListItem()
				{
					Label = "適合率が高い順",
					Sort = Sort.Relation,
					Order = Order.Descending,
				}
				,
				new SearchSortOptionListItem()
				{
					Label = "適合率が低い順",
					Sort = Sort.Relation,
					Order = Order.Ascending,
				}
				,
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

		public ReactiveCommand ShowSearchHistoryCommand { get; private set; }

		public ReactiveProperty<HohoemaViewModelBase> ContentVM { get; private set; }

		public List<SearchSortOptionListItem> SearchOptioinItems { get; private set; }

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

			TargetListItems = new List<SearchTarget>()
			{
				SearchTarget.Keyword,
				SearchTarget.Tag,
				SearchTarget.Mylist,
			};

			SelectedTarget = new ReactiveProperty<SearchTarget>(TargetListItems[0])
				.AddTo(_CompositeDisposable);

			SelectedSearchOption = new ReactiveProperty<SearchSortOptionListItem>()
				.AddTo(_CompositeDisposable);

			SelectedTarget.Subscribe(x => 
			{
				RaiseSearchTargetFlags();

				switch (x)
				{
					case SearchTarget.Keyword:
						SearchOptioinItems = VideoSearchOptionListItems;
						break;
					case SearchTarget.Tag:
						SearchOptioinItems = VideoSearchOptionListItems;
						break;
					case SearchTarget.Mylist:
						SearchOptioinItems = MylistSearchOptionListItems;
						break;
					case SearchTarget.Community:
						break;
					case SearchTarget.Niconama:
						break;
					default:
						break;
				}

				OnPropertyChanged(nameof(SearchOptioinItems));

				SelectedSearchOption.Value = SearchOptioinItems.FirstOrDefault();
			});



			DoSearchCommand =
				SearchText.Select(x => !String.IsNullOrEmpty(x))
				.ToReactiveCommand()
				.AddTo(_CompositeDisposable);

			DoSearchCommand.Subscribe(_ =>
			{
				if (SearchText.Value.Length == 0) { return; }

				// キーワードを検索履歴を記録
				SearchHistoryDb.Searched(SearchText.Value, SelectedTarget.Value);

				var searchOption = new SearchOption()
				{
					Keyword = SearchText.Value,
					SearchTarget = SelectedTarget.Value,
					Sort = SelectedSearchOption.Value.Sort,
					Order = SelectedSearchOption.Value.Order
				};

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

			ShowSearchHistoryCommand = new ReactiveCommand();
			ShowSearchHistoryCommand.Subscribe(_ => 
			{
				PageManager.OpenPage(HohoemaPageType.Search);
			});

		}

		internal void OnSearchHistorySelected(SearchHistory item)
		{
			SearchText.Value = item.Keyword;
			SelectedTarget.Value = TargetListItems.Single(x => x == item.Target);
		}

		public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			var prevSearchOption = RequireSearchOption;
			RequireSearchOption = null;
			if (e.Parameter is string)
			{
				RequireSearchOption = SearchOption.FromParameterString(e.Parameter as string);
			}
			else if (e.NavigationMode == NavigationMode.New || e.NavigationMode == NavigationMode.Refresh)
			{
				// バック・フォワード以外では常に検索履歴（EmptySearchPage）を表示させたいので
				// 検索オプションをクリア
				RequireSearchOption = null;
			}
			
			

			// ContentVM側のページタイトルが後で呼び出されるように、SearchPage側を先に呼び出す
			base.OnNavigatedTo(e, viewModelState);


			if (!(prevSearchOption?.Equals(RequireSearchOption) ?? false))
			{
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
						contentVM = new MylistSearchPageContentViewModel(
							HohoemaApp
							, PageManager
							, RequireSearchOption
							);
						break;
					case SearchTarget.Community:
						break;
					case SearchTarget.Niconama:
						break;
					default:
						contentVM = new EmptySearchPageContentViewModel(
							HohoemaApp
							, PageManager
							, this
							);
						break;
				}

				if (contentVM == null)
				{
					throw new NotSupportedException($"not support SearchPageContent type : {RequireSearchOption.SearchTarget}");
				}

				ContentVM.Value = contentVM;
			}

			ContentVM.Value?.OnNavigatedTo(e, viewModelState);
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

	public class VideoSearchSource : HohoemaVideoPreloadingIncrementalSourceBase<VideoInfoControlViewModel>
	{
		public int MaxPageCount { get; private set; }

		HohoemaApp _HohoemaApp;
		PageManager _PageManager;
		public SearchOption SearchOption { get; private set; }


		public VideoSearchSource(SearchOption searchOption, HohoemaApp hohoemaApp, PageManager pageManager)
			: base(hohoemaApp
				  , $"Search_{searchOption.SearchTarget.ToString()}_{searchOption.Keyword}"
				  )
		{
			_HohoemaApp = hohoemaApp;
			_PageManager = pageManager;
			SearchOption = searchOption;
		}

		VideoListingResponse res;

		#region Implements HohoemaPreloadingIncrementalSourceBase		

		protected override async Task<IEnumerable<NicoVideo>> PreloadNicoVideo(int start, int count)
		{
			// 最初の検索結果だけ先行してThumbnail情報を読みこませる
//			VideoListingResponse res = null;
			if (SearchOption.SearchTarget == SearchTarget.Keyword)
			{
				res = await _HohoemaApp.ContentFinder.GetKeywordSearch(SearchOption.Keyword, (uint)start, (uint)count);
			}
			else if (SearchOption.SearchTarget == SearchTarget.Tag)
			{
				res = await _HohoemaApp.ContentFinder.GetTagSearch(SearchOption.Keyword, (uint)start, (uint)count);
			}

			if (res == null && res.VideoInfoItems == null)
			{
				return Enumerable.Empty<NicoVideo>();
			}
			else
			{
				List<NicoVideo> videos = new List<NicoVideo>();
				foreach (var item in res.VideoInfoItems)
				{
					var nicoVideo = await ToNicoVideo(item.Video.Id);

					nicoVideo.PreSetTitle(item.Video.Title);
					nicoVideo.PreSetPostAt(item.Video.UploadTime);
					nicoVideo.PreSetThumbnailUrl(item.Video.ThumbnailUrl.AbsoluteUri);
					nicoVideo.PreSetVideoLength(item.Video.Length);
					nicoVideo.PreSetViewCount(item.Video.ViewCount);
					nicoVideo.PreSetCommentCount(item.Thread.GetCommentCount());
					nicoVideo.PreSetMylistCount(item.Video.MylistCount);

					videos.Add(nicoVideo);
				}

				return videos;
			}
		}


		protected override async Task<int> ResetSourceImpl()
		{
			int totalCount = 0;
			if (SearchOption.SearchTarget == SearchTarget.Keyword)
			{
				var res = await _HohoemaApp.ContentFinder.GetKeywordSearch(SearchOption.Keyword, 0, 2);
				totalCount = (int)res.GetTotalCount();

			}
			else if (SearchOption.SearchTarget == SearchTarget.Tag)
			{
				var res = await _HohoemaApp.ContentFinder.GetTagSearch(SearchOption.Keyword, 0, 2);
				totalCount = (int)res.GetTotalCount();
			}

			return totalCount;
		}




		protected override VideoInfoControlViewModel NicoVideoToTemplatedItem(
			NicoVideo sourceItem
			, int index
			)
		{
			return new VideoInfoControlViewModel(sourceItem, _PageManager);
		}

		#endregion


	}



	public class SearchHistoryListItem : SelectableItem<SearchHistory>
	{
		public string Keyword { get; private set; }
		public SearchTarget Target { get; private set; }



		public SearchHistoryListItem(SearchHistory source, Action<SearchHistory> selectedAction) : base(source, selectedAction)
		{
			Keyword = source.Keyword;
			Target = source.Target;
		}
	}
}
