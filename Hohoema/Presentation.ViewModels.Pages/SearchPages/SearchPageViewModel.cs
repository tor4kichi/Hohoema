using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hohoema.Models.Domain;
using System.Collections.ObjectModel;
using Reactive.Bindings;
using Prism.Commands;
using Prism.Mvvm;
using System.Reactive.Linq;
using System.Diagnostics;
using Reactive.Bindings.Extensions;
using Mntone.Nico2;
using Mntone.Nico2.Searches.Video;
using Mntone.Nico2.Searches.Community;
using Mntone.Nico2.Searches.Live;
using Hohoema.Presentation.Services.Page;

using Unity;
using Hohoema.Presentation.Services;
using Hohoema.Models.UseCase;
using System.Runtime.CompilerServices;
using System.Threading;
using NiconicoSession = Hohoema.Models.Domain.NiconicoSession;
using Hohoema.Models.Domain.Niconico.Search;
using Hohoema.Models.Domain.PageNavigation;
using Hohoema.Presentation.ViewModels.VideoListPage;
using Prism.Navigation;
using I18NPortable;

namespace Hohoema.Presentation.ViewModels.Pages.SearchPages
{
    public class SearchPageViewModel : HohoemaViewModelBase, ITitleUpdatablePage, IPinablePage
    {

		public ApplicationLayoutManager ApplicationLayoutManager { get; }
		public NiconicoSession NiconicoSession { get; }
		public SearchProvider SearchProvider { get; }
		public PageManager PageManager { get; }
		private readonly SearchHistoryRepository _searchHistoryRepository;


		public ISearchPagePayloadContent RequireSearchOption { get; private set; }


		public ReactiveCommand DoSearchCommand { get; private set; }

		public ReactiveProperty<string> SearchText { get; private set; }
		public List<SearchTarget> TargetListItems { get; private set; }
		public ReactiveProperty<SearchTarget> SelectedTarget { get; private set; }

		private static SearchTarget _LastSelectedTarget;
		private static string _LastKeyword;

		public ReactiveProperty<bool> IsNavigationFailed { get; }
		public ReactiveProperty<string> NavigationFailedReason { get; }



		public ObservableCollection<SearchHistoryListItemViewModel> SearchHistoryItems { get; private set; } = new ObservableCollection<SearchHistoryListItemViewModel>();


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


		private DelegateCommand _DeleteAllSearchHistoryCommand;
		public DelegateCommand DeleteAllSearchHistoryCommand
		{
			get
			{
				return _DeleteAllSearchHistoryCommand
					?? (_DeleteAllSearchHistoryCommand = new DelegateCommand(() =>
					{
						_searchHistoryRepository.Clear();

						SearchHistoryItems.Clear();
						RaisePropertyChanged(nameof(SearchHistoryItems));
					},
					() => _searchHistoryRepository.Count() > 0
					));
			}
		}

		private DelegateCommand<SearchHistoryListItemViewModel> _SearchHistoryItemCommand;
		public DelegateCommand<SearchHistoryListItemViewModel> SearchHistoryItemCommand
		{
			get
			{
				return _SearchHistoryItemCommand
					?? (_SearchHistoryItemCommand = new DelegateCommand<SearchHistoryListItemViewModel>((item) =>
					{
						SearchText.Value = item.Keyword;
						if (DoSearchCommand.CanExecute())
                        {
							DoSearchCommand.Execute();
						}
					}
					));
			}
		}


		private DelegateCommand<SearchHistory> _DeleteSearchHistoryItemCommand;
		public DelegateCommand<SearchHistory> DeleteSearchHistoryItemCommand
		{
			get
			{
				return _DeleteSearchHistoryItemCommand
					?? (_DeleteSearchHistoryItemCommand = new DelegateCommand<SearchHistory>((item) =>
					{
						_searchHistoryRepository.Remove(item.Keyword, item.Target);
						var itemVM = SearchHistoryItems.FirstOrDefault(x => x.Keyword == item.Keyword && x.Target == item.Target);
						if (itemVM != null)
						{
							SearchHistoryItems.Remove(itemVM);
						}
					}
					));
			}
		}


		public INavigationService NavigationService => Views.Pages.SearchPages.SearchPage.ContentNavigationService;

		public SearchPageViewModel(
			ApplicationLayoutManager applicationLayoutManager,
			NiconicoSession niconicoSession,
            SearchProvider searchProvider,
            PageManager pageManager,
			SearchHistoryRepository searchHistoryRepository
            )
        {
			ApplicationLayoutManager = applicationLayoutManager;
			NiconicoSession = niconicoSession;
            SearchProvider = searchProvider;
            PageManager = pageManager;
            _searchHistoryRepository = searchHistoryRepository;
            HashSet<string> HistoryKeyword = new HashSet<string>();
            foreach (var item in _searchHistoryRepository.ReadAllItems().OrderByDescending(x => x.LastUpdated))
            {
                if (HistoryKeyword.Contains(item.Keyword))
                {
                    continue;
                }

                SearchHistoryItems.Add(new SearchHistoryListItemViewModel(item, this));
                HistoryKeyword.Add(item.Keyword);
            }

            SearchText = new ReactiveProperty<string>(_LastKeyword)
                .AddTo(_CompositeDisposable);

            TargetListItems = new List<SearchTarget>()
            {
                SearchTarget.Keyword,
                SearchTarget.Tag,
                SearchTarget.Niconama,
                SearchTarget.Mylist,
                SearchTarget.Community,
            };

            SelectedTarget = new ReactiveProperty<SearchTarget>(_LastSelectedTarget)
                .AddTo(_CompositeDisposable);

            DoSearchCommand = new ReactiveCommand()
                .AddTo(_CompositeDisposable);
#if DEBUG
			SearchText.Subscribe(x =>
            {
                Debug.WriteLine($"検索：{x}");
            });
#endif

#if DEBUG
			DoSearchCommand.CanExecuteChangedAsObservable()
                .Subscribe(x =>
                {
                    Debug.WriteLine(DoSearchCommand.CanExecute());
                });
#endif

			DoSearchCommand.Subscribe(async _ =>
            {
				await Task.Delay(50);

                if (SearchText.Value?.Length == 0) { return; }

				if (_LastSelectedTarget == SelectedTarget.Value && _LastKeyword == SearchText.Value) { return; }

				// 検索結果を表示
				PageManager.Search(SelectedTarget.Value, SearchText.Value);

                var searched = _searchHistoryRepository.Searched(SearchText.Value, SelectedTarget.Value);

                var oldSearchHistory = SearchHistoryItems.FirstOrDefault(x => x.Keyword == SearchText.Value);
                if (oldSearchHistory != null)
                {
                    SearchHistoryItems.Remove(oldSearchHistory);
                }
                SearchHistoryItems.Insert(0, new SearchHistoryListItemViewModel(searched, this));

            })
            .AddTo(_CompositeDisposable);

			IsNavigationFailed = new ReactiveProperty<bool>();
		    NavigationFailedReason = new ReactiveProperty<string>();
		}       

        public override async void OnNavigatedTo(INavigationParameters parameters)
        {
			IsNavigationFailed.Value = false;
			NavigationFailedReason.Value = null;

			try
            {
				string keyword = null;
				if (parameters.TryGetValue("keyword", out keyword))
				{
					keyword = Uri.UnescapeDataString(keyword);
				}


				SearchTarget target = SearchTarget.Keyword;
				if (!parameters.TryGetValue("service", out string modeString)
					|| !Enum.TryParse<SearchTarget>(modeString, out target)
					)
				{
					Debug.Assert(true);

					target = SearchTarget.Keyword;
				}
				
				var pageName = target switch
				{
					SearchTarget.Keyword => nameof(Views.Pages.SearchPages.SearchResultKeywordPage),
					SearchTarget.Tag => nameof(Views.Pages.SearchPages.SearchResultTagPage),
					SearchTarget.Niconama => nameof(Views.Pages.SearchPages.SearchResultLivePage),
					SearchTarget.Mylist => nameof(Views.Pages.SearchPages.SearchResultMylistPage),
					SearchTarget.Community => nameof(Views.Pages.SearchPages.SearchResultCommunityPage),
					_ => null
				};

				if (pageName != null && keyword != null)
                {
					var result = await NavigationService.NavigateAsync(pageName, ("keyword", keyword));
					if (!result.Success)
					{
						throw result.Exception;
					}
				}

				SearchText.Value = keyword;
				SelectedTarget.Value = target;

				_LastSelectedTarget = target;
				_LastKeyword = keyword;
			}
			catch (Exception e)
            {
				IsNavigationFailed.Value = true;
#if DEBUG
				NavigationFailedReason.Value = e.Message;
#endif
				Debug.WriteLine(e.ToString());
			}

			base.OnNavigatedTo(parameters);
        }

        public IObservable<string> GetTitleObservable()
        {
			return SearchText.Select(x => $"{"Search".Translate()} '{x}'");
        }

        public HohoemaPin GetPin()
        {
			if (_LastKeyword == null) { return null; }
			
			return new HohoemaPin()
			{
				Label = _LastKeyword + $" - {_LastSelectedTarget.Translate()}",
				PageType = HohoemaPageType.Search,
				Parameter = $"keyword={System.Net.WebUtility.UrlEncode(_LastKeyword)}&service={SelectedTarget.Value}",
			};
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
					return null;
					//return new LiveSearchOptionViewModel();
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
				item.Label = Services.Helpers.SortHelper.ToCulturizedText(item.Sort, item.Order);
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

	
	//public class LiveSearchOptionViewModel : SearchOptionViewModelBase
	//{
	//	public class LiveSearchSortOptionListItem
	//	{
	//		public string Label { get; set; }
	//		public NicoliveSearchSort Sort { get; set; }
	//		public Order Order { get; set; }
	//	}

	//	public class LiveSearchModeOptionListItem
	//	{
	//		public string Label { get; set; }
	//		public NicoliveSearchMode? Mode { get; set; }
	//	}

	//	public class LiveSearchProviderOptionListItem
	//	{
	//		public string Label { get; set; }
	//		public Mntone.Nico2.Live.CommunityType? Provider { get; set; }
	//	}

	//	public static IReadOnlyList<LiveSearchSortOptionListItem> LiveSearchSortOptionListItems { get; private set; }
	//	public static IReadOnlyList<LiveSearchModeOptionListItem> LiveSearchModeOptionListItems { get; private set; }
	//	public static IReadOnlyList<LiveSearchProviderOptionListItem> LiveSearchProviderOptionListItems { get; private set; }

	//	static LiveSearchOptionViewModel()
	//	{
	//		var sortList = new[]
	//		{
	//			NicoliveSearchSort.Recent,
	//			NicoliveSearchSort.Comment,
	//		};

	//		LiveSearchSortOptionListItems = sortList.SelectMany(x =>
	//		{
	//			return new List<LiveSearchSortOptionListItem>()
	//			{
 //                   new LiveSearchSortOptionListItem()
 //                   {
 //                       Sort = x,
 //                       Order = Order.Ascending,
 //                   },
 //                   new LiveSearchSortOptionListItem()
 //                   {
 //                       Sort = x,
 //                       Order = Order.Descending,
 //                   },
 //               };
	//		})
	//		.ToList();

	//		foreach (var item in LiveSearchSortOptionListItems)
	//		{
	//			item.Label = Services.Helpers.SortHelper.ToCulturizedText(item.Sort, item.Order);
	//		}


	//		LiveSearchModeOptionListItems = new List<LiveSearchModeOptionListItem>()
	//		{
	//			new LiveSearchModeOptionListItem()
	//			{
	//				Label = "放送中",
	//				Mode = NicoliveSearchMode.OnAir
	//			},
	//			new LiveSearchModeOptionListItem()
	//			{
	//				Label = "放送予定",
	//				Mode = NicoliveSearchMode.Reserved
	//			},
 //               /*
	//			new LiveSearchModeOptionListItem()
	//			{
	//				Label = "放送終了",
	//				Mode = NicoliveSearchMode.Closed
	//			},
	//			new LiveSearchModeOptionListItem()
	//			{
	//				Label = "すべて",
	//				Mode = null
	//			},
 //               */
	//		};


	//		LiveSearchProviderOptionListItems = new List<LiveSearchProviderOptionListItem>()
	//		{
	//			new LiveSearchProviderOptionListItem()
	//			{
	//				Label = "すべて",
	//				Provider = null,
	//			},

	//			new LiveSearchProviderOptionListItem()
	//			{
	//				Label = "公式",
	//				Provider = Mntone.Nico2.Live.CommunityType.Official,
	//			},
	//			new LiveSearchProviderOptionListItem()
	//			{
	//				Label = "チャンネル",
	//				Provider = Mntone.Nico2.Live.CommunityType.Channel,
	//			},
	//			new LiveSearchProviderOptionListItem()
	//			{
	//				Label = "ユーザー",
	//				Provider = Mntone.Nico2.Live.CommunityType.Community,
	//			},
							
	//		};
	//	}

	//	public ReactiveProperty<LiveSearchSortOptionListItem> SelectedSearchSort { get; private set; }
	//	public ReactiveProperty<LiveSearchModeOptionListItem> SelectedSearchMode { get; private set; }
	//	public ReactiveProperty<bool> IsTagSearch { get; private set; }
	//	public ReactiveProperty<LiveSearchProviderOptionListItem> SelectedProvider { get; private set; }

	//	public LiveSearchOptionViewModel()
	//	{
	//		SelectedSearchSort = new ReactiveProperty<LiveSearchSortOptionListItem>(LiveSearchSortOptionListItems[0]);
	//		SelectedSearchMode = new ReactiveProperty<LiveSearchModeOptionListItem>(LiveSearchModeOptionListItems[0]);
	//		SelectedProvider = new ReactiveProperty<LiveSearchProviderOptionListItem>(LiveSearchProviderOptionListItems[0]);
	//		IsTagSearch = new ReactiveProperty<bool>(false);
	//	}

	//	public override ISearchPagePayloadContent MakePayload()
	//	{
	//		return new LiveSearchPagePayloadContent()
	//		{
	//			Keyword = Keyword,
	//			Sort = SelectedSearchSort.Value.Sort,
	//			Order = SelectedSearchSort.Value.Order,
	//			Mode = SelectedSearchMode.Value.Mode,
	//			Provider = SelectedProvider.Value.Provider,
	//			IsTagSearch = IsTagSearch.Value
	//		};
	//	}
	//}




	public class VideoSearchSource : HohoemaIncrementalSourceBase<VideoInfoControlViewModel>
	{
        public VideoSearchSource(string keyword, bool isTagSearch, Sort sort, Order order, SearchProvider searchProvider)
        {
            Keyword = keyword;
            IsTagSearch = isTagSearch;
			SearchSort = sort;
			SearchOrder = order;
            SearchProvider = searchProvider;
        }

		public string Keyword { get; }

		public bool IsTagSearch { get;  }

		public Sort SearchSort { get;  }
		public Order SearchOrder { get; }

		public SearchProvider SearchProvider { get; }
		

		

        protected override async IAsyncEnumerable<VideoInfoControlViewModel> GetPagedItemsImpl(int head, int count, [EnumeratorCancellation] CancellationToken ct = default)
        {
            VideoListingResponse res = null;
            if (!IsTagSearch)
            {
                res = await SearchProvider.GetKeywordSearch(Keyword, (uint)head, (uint)count, SearchSort, SearchOrder);
            }
            else 
            {
                res = await SearchProvider.GetTagSearch(Keyword, (uint)head, (uint)count, SearchSort, SearchOrder);
            }

			ct.ThrowIfCancellationRequested();

            if (res == null || res.VideoInfoItems == null)
            {
                
            }
            else
            {
                foreach (var item in res.VideoInfoItems.Where(x => x != null))
                {
                    var vm = new VideoInfoControlViewModel(item.Video.Id);

                    vm.SetupDisplay(item);
					
                    yield return vm;

					_ = vm.InitializeAsync(ct).ConfigureAwait(false);

					ct.ThrowIfCancellationRequested();
                }
            }
        }

        protected override async Task<int> ResetSourceImpl()
        {
            int totalCount = 0;
            if (!IsTagSearch)
            {
                var res = await SearchProvider.GetKeywordSearch(Keyword, 0, 2, SearchSort, SearchOrder);
                totalCount = (int)res.GetTotalCount();

            }
            else 
            {
                var res = await SearchProvider.GetTagSearch(Keyword, 0, 2, SearchSort, SearchOrder);
                totalCount = (int)res.GetTotalCount();
            }

            return totalCount;
        }
    }



	public class SearchHistoryListItemViewModel : ISearchHistory
    {
        public SearchHistory SearchHistory { get; }
        public string Keyword { get; private set; }
		public SearchTarget Target { get; private set; }

        SearchPageViewModel SearchPageVM { get; }

        public SearchHistoryListItemViewModel(SearchHistory source, SearchPageViewModel parentVM)
		{
            SearchHistory = source;
            SearchPageVM = parentVM;
            Keyword = source.Keyword;
			Target = source.Target;
		}

        
        private DelegateCommand _DeleteSearchHistoryItemCommand;
        public DelegateCommand DeleteSearchHistoryItemCommand
        {
            get
            {
                return _DeleteSearchHistoryItemCommand
                    ?? (_DeleteSearchHistoryItemCommand = new DelegateCommand(() =>
                    {
                        SearchPageVM.DeleteSearchHistoryItemCommand.Execute(SearchHistory);
                    }
                    ));
            }
        }
    }
}
