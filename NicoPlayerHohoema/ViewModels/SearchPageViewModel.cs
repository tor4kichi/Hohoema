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
using Mntone.Nico2.Searches.Community;
using System.Runtime.Serialization;
using Mntone.Nico2.Searches.Live;

namespace NicoPlayerHohoema.ViewModels
{
	public class SearchPageViewModel : HohoemaViewModelBase
	{
		Views.Service.MylistRegistrationDialogService _MylistDialogService;
		public ISearchPagePayloadContent RequireSearchOption { get; private set; }


		public ReactiveCommand DoSearchCommand { get; private set; }

		public ReactiveProperty<string> SearchText { get; private set; }
		public List<SearchTarget> TargetListItems { get; private set; }
		public ReactiveProperty<SearchTarget> SelectedTarget { get; private set; }
		
		public ReactiveCommand ShowSearchHistoryCommand { get; private set; }

		public ReactiveProperty<HohoemaViewModelBase> ContentVM { get; private set; }

		public Dictionary<SearchTarget, SearchOptionViewModelBase> SearchOptionVMDict { get; private set; }
		public ReactiveProperty<SearchOptionViewModelBase> SearchOptionVM { get; private set; }

		public bool IsSearchKeyword => RequireSearchOption is KeywordSearchPagePayloadContent;

		public bool IsSearchTag => RequireSearchOption is TagSearchPagePayloadContent;

		public bool IsSearchMylist => RequireSearchOption is MylistSearchPagePayloadContent;

		public bool IsSearchCommunity => RequireSearchOption is CommunitySearchPagePayloadContent;

		public bool IsSearchNiconama => RequireSearchOption is LiveSearchPagePayloadContent;

		private void RaiseSearchTargetFlags()
		{
			OnPropertyChanged(nameof(IsSearchKeyword));
			OnPropertyChanged(nameof(IsSearchTag));
			OnPropertyChanged(nameof(IsSearchMylist));
			OnPropertyChanged(nameof(IsSearchCommunity));
			OnPropertyChanged(nameof(IsSearchNiconama));
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
				SearchTarget.Community,
				SearchTarget.Niconama,
			};

			SelectedTarget = new ReactiveProperty<SearchTarget>(TargetListItems[0])
				.AddTo(_CompositeDisposable);

			SearchOptionVM = new ReactiveProperty<SearchOptionViewModelBase>();
			SearchOptionVMDict = new Dictionary<SearchTarget, SearchOptionViewModelBase>();

			SelectedTarget.Subscribe(x => 
			{
				RaiseSearchTargetFlags();

				var keyword = SearchOptionVM.Value?.Keyword ?? "";
				SearchOptionViewModelBase searchOptionVM = null;
				if (SearchOptionVMDict.ContainsKey(x))
				{
					searchOptionVM = SearchOptionVMDict[x];
				}
				else
				{
					searchOptionVM = SearchOptioniViewModelHelper.CreateFromTarget(x);
					SearchOptionVMDict.Add(x, searchOptionVM);
				}

				searchOptionVM.Keyword = keyword;

				SearchOptionVM.Value = searchOptionVM;

				
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


				// Note: Keywordの管理はSearchPage側で行うべき？
				SearchOptionVM.Value.Keyword = SearchText.Value;


				var searchOption = SearchOptionVM.Value.MakePayload();

				var payload = new SearchPagePayload(searchOption);
				var searchPageParameter = payload.ToParameterString();
				var isEmptyPage = RequireSearchOption == null;

				// 検索結果を表示
				PageManager.OpenPage(
					HohoemaPageType.Search,
					searchPageParameter
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
				var payload = SearchPagePayload.FromParameterString<SearchPagePayload>(e.Parameter as string);
				RequireSearchOption = payload.GetContentImpl();
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
				if (IsSearchKeyword)
				{
					contentVM = new KeywordSearchPageContentViewModel(
							RequireSearchOption as KeywordSearchPagePayloadContent
							, HohoemaApp
							, PageManager
							, _MylistDialogService
							);
				}
				else if (IsSearchTag)
				{
					contentVM = new TagSearchPageContentViewModel(
							RequireSearchOption as TagSearchPagePayloadContent
							, HohoemaApp
							, PageManager
							, _MylistDialogService
							);
				}
				else if (IsSearchMylist)
				{
					contentVM = new MylistSearchPageContentViewModel(
							RequireSearchOption as MylistSearchPagePayloadContent
							, HohoemaApp
							, PageManager
							);
				}
				else if (IsSearchCommunity)
				{
					contentVM = new CommunitySearchPageContentViewModel(
							RequireSearchOption as CommunitySearchPagePayloadContent
							, HohoemaApp
							, PageManager
							);
				}
				else if (IsSearchNiconama)
				{
					contentVM = new LiveSearchPageContentViewModel(
							RequireSearchOption as LiveSearchPagePayloadContent
							, HohoemaApp
							, PageManager
						);
				}
				else
				{
					contentVM = new EmptySearchPageContentViewModel(
							HohoemaApp
							, PageManager
							, this
							);
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



	public static class SearchOptioniViewModelHelper
	{
		public static SearchOptionViewModelBase CreateFromTarget(SearchTarget target)
		{
			switch (target)
			{
				case SearchTarget.Keyword:
					return new KeywordVideoSearchOptionViewModel();
				case SearchTarget.Tag:
					return new TagVideoSearchOptionViewModel();
				case SearchTarget.Mylist:
					return new MylistSearchOptionViewModel();
				case SearchTarget.Community:
					return new CommunitySearchOptionViewModel();
				case SearchTarget.Niconama:
					return new LiveSearchOptionViewModel();
				default:
					break;
			}

			throw new NotSupportedException();
		}
	}

	// SearchOptionのViewModel


	public abstract class SearchOptionViewModelBase : BindableBase
	{
		private string _Keyword;
		public string Keyword
		{
			get { return _Keyword; }
			set { SetProperty(ref _Keyword, value); }
		}

		public abstract ISearchPagePayloadContent MakePayload();
	}


	public abstract class VideoSearchOptionViewModelBase : SearchOptionViewModelBase
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


		public VideoSearchOptionViewModelBase()
		{
			SelectedSearchSort = new ReactiveProperty<SearchSortOptionListItem>(VideoSearchOptionListItems[0]);
		}

		
	}

	public class KeywordVideoSearchOptionViewModel : VideoSearchOptionViewModelBase
	{
		public override ISearchPagePayloadContent MakePayload()
		{
			return new KeywordSearchPagePayloadContent()
			{
				Keyword = Keyword,
				Sort = SelectedSearchSort.Value.Sort,
				Order = SelectedSearchSort.Value.Order,
			};
		}
	}

	public class TagVideoSearchOptionViewModel : VideoSearchOptionViewModelBase
	{
		public override ISearchPagePayloadContent MakePayload()
		{
			return new TagSearchPagePayloadContent()
			{
				Keyword = Keyword,
				Sort = SelectedSearchSort.Value.Sort,
				Order = SelectedSearchSort.Value.Order,
			};
		}
	}


	public class MylistSearchOptionViewModel : SearchOptionViewModelBase
	{
		public static IReadOnlyList<SearchSortOptionListItem> MylistSearchOptionListItems { get; private set; }

		static MylistSearchOptionViewModel()
		{
			#region マイリスト検索時のオプション

			MylistSearchOptionListItems = new List<SearchSortOptionListItem>()
			{
				new SearchSortOptionListItem()
				{
					Label = "人気が高い順",
					Sort = Sort.MylistPopurarity,
					Order = Order.Descending,
				}
				, new SearchSortOptionListItem()
				{
					Label = "人気が低い順",
					Sort = Sort.MylistPopurarity,
					Order = Order.Ascending,
				}
				, new SearchSortOptionListItem()
				{
					Label = "更新が新しい順",
					Sort = Sort.UpdateTime,
					Order = Order.Descending,
				}
				, new SearchSortOptionListItem()
				{
					Label = "更新が古い順",
					Sort = Sort.UpdateTime,
					Order = Order.Ascending,
				}
				, new SearchSortOptionListItem()
				{
					Label = "動画数が多い順",
					Sort = Sort.VideoCount,
					Order = Order.Descending,
				}
				, new SearchSortOptionListItem()
				{
					Label = "動画数が少ない順",
					Sort = Sort.VideoCount,
					Order = Order.Ascending,
				}
				, new SearchSortOptionListItem()
				{
					Label = "適合率が高い順",
					Sort = Sort.Relation,
					Order = Order.Descending,
				}
				, new SearchSortOptionListItem()
				{
					Label = "適合率が低い順",
					Sort = Sort.Relation,
					Order = Order.Ascending,
				}

			};

			#endregion


		}

		public ReactiveProperty<SearchSortOptionListItem> SelectedSearchSort { get; private set; }


		public MylistSearchOptionViewModel()
		{
			SelectedSearchSort = new ReactiveProperty<SearchSortOptionListItem>(MylistSearchOptionListItems[0]);
		}

		public override ISearchPagePayloadContent MakePayload()
		{
			return new MylistSearchPagePayloadContent()
			{
				Keyword = Keyword,
				Sort = SelectedSearchSort.Value.Sort,
				Order = SelectedSearchSort.Value.Order,
			};
		}
	}


	public class CommunitySearchOptionViewModel : SearchOptionViewModelBase
	{
		public class CommunitySearchSortOptionListItem
		{
			public string Label { get; set; }
			public CommunitySearchSort Sort { get; set; }
			public Order Order { get; set; }
		}

		public class CommynitySearchModeOptionListItem
		{
			public string Label { get; set; }
			public CommunitySearchMode Mode { get; set; }
		}

		public static IReadOnlyList<CommunitySearchSortOptionListItem> CommunitySearchSortOptionListItems { get; private set; }
		public static IReadOnlyList<CommynitySearchModeOptionListItem> CommunitySearchModeOptionListItems { get; private set; }

		static CommunitySearchOptionViewModel()
		{
			var sortList = new[]
			{
				CommunitySearchSort.CreatedAt,
				CommunitySearchSort.UpdateAt,
				CommunitySearchSort.CommunityLevel,
				CommunitySearchSort.VideoCount,
				CommunitySearchSort.MemberCount
			};

			CommunitySearchSortOptionListItems = sortList.SelectMany(x => 
			{
				return new List<CommunitySearchSortOptionListItem>()
				{
					new CommunitySearchSortOptionListItem()
					{
						Sort = x,
						Order = Order.Descending,
					},
					new CommunitySearchSortOptionListItem()
					{
						Sort = x,
						Order = Order.Ascending,
					},
				};
			})
			.ToList();

			foreach (var item in CommunitySearchSortOptionListItems)
			{
				item.Label = Util.SortHelper.ToCulturizedText(item.Sort, item.Order);
			}


			CommunitySearchModeOptionListItems = new List<CommynitySearchModeOptionListItem>()
			{
				new CommynitySearchModeOptionListItem()
				{
					Label = "キーワードで探す",
					Mode = CommunitySearchMode.Keyword
				},
				new CommynitySearchModeOptionListItem()
				{
					Label = "タグで探す",
					Mode = CommunitySearchMode.Tag
				},
			};
		}

		public ReactiveProperty<CommunitySearchSortOptionListItem> SelectedSearchSort { get; private set; }
		public ReactiveProperty<CommynitySearchModeOptionListItem> SelectedSearchMode { get; private set; }

		public CommunitySearchOptionViewModel()
		{
			SelectedSearchSort = new ReactiveProperty<CommunitySearchSortOptionListItem>(CommunitySearchSortOptionListItems[0]);
			SelectedSearchMode = new ReactiveProperty<CommynitySearchModeOptionListItem>(CommunitySearchModeOptionListItems[0]);
		}

		public override ISearchPagePayloadContent MakePayload()
		{
			return new CommunitySearchPagePayloadContent()
			{
				Keyword = Keyword,
				Sort = SelectedSearchSort.Value.Sort,
				Order = SelectedSearchSort.Value.Order,
				Mode = SelectedSearchMode.Value.Mode,
			};
		}
	}



	public class LiveSearchOptionViewModel : SearchOptionViewModelBase
	{
		public class LiveSearchSortOptionListItem
		{
			public string Label { get; set; }
			public NicoliveSearchSort Sort { get; set; }
			public Order Order { get; set; }
		}

		public class LiveSearchModeOptionListItem
		{
			public string Label { get; set; }
			public NicoliveSearchMode? Mode { get; set; }
		}

		public static IReadOnlyList<LiveSearchSortOptionListItem> LiveSearchSortOptionListItems { get; private set; }
		public static IReadOnlyList<LiveSearchModeOptionListItem> LiveSearchModeOptionListItems { get; private set; }

		static LiveSearchOptionViewModel()
		{
			var sortList = new[]
			{
				NicoliveSearchSort.Recent,
				NicoliveSearchSort.Comment,
			};

			LiveSearchSortOptionListItems = sortList.SelectMany(x =>
			{
				return new List<LiveSearchSortOptionListItem>()
				{
					new LiveSearchSortOptionListItem()
					{
						Sort = x,
						Order = Order.Descending,
					},
					new LiveSearchSortOptionListItem()
					{
						Sort = x,
						Order = Order.Ascending,
					},
				};
			})
			.ToList();

			foreach (var item in LiveSearchSortOptionListItems)
			{
				item.Label = Util.SortHelper.ToCulturizedText(item.Sort, item.Order);
			}


			LiveSearchModeOptionListItems = new List<LiveSearchModeOptionListItem>()
			{
				new LiveSearchModeOptionListItem()
				{
					Label = "放送中",
					Mode = NicoliveSearchMode.OnAir
				},
				new LiveSearchModeOptionListItem()
				{
					Label = "放送予定",
					Mode = NicoliveSearchMode.Reserved
				},
				new LiveSearchModeOptionListItem()
				{
					Label = "放送終了",
					Mode = NicoliveSearchMode.Closed
				},
				new LiveSearchModeOptionListItem()
				{
					Label = "すべて",
					Mode = null
				},
			};
		}

		public ReactiveProperty<LiveSearchSortOptionListItem> SelectedSearchSort { get; private set; }
		public ReactiveProperty<LiveSearchModeOptionListItem> SelectedSearchMode { get; private set; }
		public ReactiveProperty<bool> IsTagSearch { get; private set; }

		public LiveSearchOptionViewModel()
		{
			SelectedSearchSort = new ReactiveProperty<LiveSearchSortOptionListItem>(LiveSearchSortOptionListItems[0]);
			SelectedSearchMode = new ReactiveProperty<LiveSearchModeOptionListItem>(LiveSearchModeOptionListItems[0]);
			IsTagSearch = new ReactiveProperty<bool>(false);
		}

		public override ISearchPagePayloadContent MakePayload()
		{
			return new LiveSearchPagePayloadContent()
			{
				Keyword = Keyword,
				Sort = SelectedSearchSort.Value.Sort,
				Order = SelectedSearchSort.Value.Order,
				Mode = SelectedSearchMode.Value.Mode,
				IsTagSearch = IsTagSearch.Value
			};
		}
	}




	public class VideoSearchSource : HohoemaVideoPreloadingIncrementalSourceBase<VideoInfoControlViewModel>
	{
		public int MaxPageCount { get; private set; }

		HohoemaApp _HohoemaApp;
		PageManager _PageManager;
		public VideoSearchOption SearchOption { get; private set; }

		public VideoSearchSource(VideoSearchOption searchOption, HohoemaApp hohoemaApp, PageManager pageManager)
			: base(hohoemaApp
				  , $"Search_{searchOption.SearchTarget}_{searchOption.Keyword}"
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
				res = await _HohoemaApp.ContentFinder.GetKeywordSearch(SearchOption.Keyword, (uint)start, (uint)count, SearchOption.Sort, SearchOption.Order);
			}
			else if (SearchOption.SearchTarget == SearchTarget.Tag)
			{
				res = await _HohoemaApp.ContentFinder.GetTagSearch(SearchOption.Keyword, (uint)start, (uint)count, SearchOption.Sort, SearchOption.Order);
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
				var res = await _HohoemaApp.ContentFinder.GetKeywordSearch(SearchOption.Keyword, 0, 2, SearchOption.Sort, SearchOption.Order);
				totalCount = (int)res.GetTotalCount();

			}
			else if (SearchOption.SearchTarget == SearchTarget.Tag)
			{
				var res = await _HohoemaApp.ContentFinder.GetTagSearch(SearchOption.Keyword, 0, 2, SearchOption.Sort, SearchOption.Order);
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
