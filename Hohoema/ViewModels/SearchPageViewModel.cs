using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hohoema.Models;
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
using Hohoema.Services.Page;
using Hohoema.Models.Provider;
using Unity;
using Hohoema.Services;
using Hohoema.UseCase;
using System.Threading;
using System.Runtime.CompilerServices;

namespace Hohoema.ViewModels
{
    public class SearchPageViewModel : HohoemaViewModelBase
    {
        public SearchPageViewModel(
			ApplicationLayoutManager applicationLayoutManager,
			Models.NiconicoSession niconicoSession,
            SearchProvider searchProvider,
            Services.PageManager pageManager
            )
        {
			ApplicationLayoutManager = applicationLayoutManager;
			NiconicoSession = niconicoSession;
            SearchProvider = searchProvider;
            PageManager = pageManager;
            HashSet<string> HistoryKeyword = new HashSet<string>();
            foreach (var item in Database.SearchHistoryDb.GetAll().OrderByDescending(x => x.LastUpdated))
            {
                if (HistoryKeyword.Contains(item.Keyword))
                {
                    continue;
                }

                SearchHistoryItems.Add(new SearchHistoryListItem(item, this));
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

            SearchText.Subscribe(x =>
            {
                Debug.WriteLine($"検索：{x}");
            });


            DoSearchCommand.CanExecuteChangedAsObservable()
                .Subscribe(x =>
                {
                    Debug.WriteLine(DoSearchCommand.CanExecute());
                });
            DoSearchCommand.Subscribe(_ =>
            {
                if (SearchText.Value.Length == 0) { return; }

                // Note: Keywordの管理はSearchPage側で行うべき？
                SearchOptionVM.Value.Keyword = SearchText.Value;

                var searchOption = SearchOptionVM.Value.MakePayload();

                // 検索結果を表示
                PageManager.Search(searchOption.SearchTarget, searchOption.Keyword);

                var searched = Database.SearchHistoryDb.Searched(SearchText.Value, SelectedTarget.Value);

                var oldSearchHistory = SearchHistoryItems.FirstOrDefault(x => x.Keyword == SearchText.Value);
                if (oldSearchHistory != null)
                {
                    SearchHistoryItems.Remove(oldSearchHistory);
                }
                SearchHistoryItems.Insert(0, new SearchHistoryListItem(searched, this));

            })
            .AddTo(_CompositeDisposable);
        }

		public ApplicationLayoutManager ApplicationLayoutManager { get; }
		public Models.NiconicoSession NiconicoSession { get; }
        public SearchProvider SearchProvider { get; }
        public PageManager PageManager { get; }

        public ISearchPagePayloadContent RequireSearchOption { get; private set; }


		public ReactiveCommand DoSearchCommand { get; private set; }

		public ReactiveProperty<string> SearchText { get; private set; }
		public List<SearchTarget> TargetListItems { get; private set; }
		public ReactiveProperty<SearchTarget> SelectedTarget { get; private set; }

        private static SearchTarget _LastSelectedTarget;
        private static string _LastKeyword;

        public Dictionary<SearchTarget, SearchOptionViewModelBase> SearchOptionVMDict { get; private set; }
		public ReactiveProperty<SearchOptionViewModelBase> SearchOptionVM { get; private set; }

		public bool IsSearchKeyword => RequireSearchOption is KeywordSearchPagePayloadContent;

		public bool IsSearchTag => RequireSearchOption is TagSearchPagePayloadContent;

		public bool IsSearchMylist => RequireSearchOption is MylistSearchPagePayloadContent;

		public bool IsSearchCommunity => RequireSearchOption is CommunitySearchPagePayloadContent;

		public bool IsSearchNiconama => RequireSearchOption is LiveSearchPagePayloadContent;

        
        public ObservableCollection<SearchHistoryListItem> SearchHistoryItems { get; private set; } = new ObservableCollection<SearchHistoryListItem>();

        private void RaiseSearchTargetFlags()
		{
			RaisePropertyChanged(nameof(IsSearchKeyword));
			RaisePropertyChanged(nameof(IsSearchTag));
			RaisePropertyChanged(nameof(IsSearchMylist));
			RaisePropertyChanged(nameof(IsSearchCommunity));
			RaisePropertyChanged(nameof(IsSearchNiconama));
		}

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
                        Database.SearchHistoryDb.Clear();

                        SearchHistoryItems.Clear();
                        RaisePropertyChanged(nameof(SearchHistoryItems));
                    },
                    () => Database.SearchHistoryDb.Count() > 0
                    ));
            }
        }

        private DelegateCommand<SearchHistoryListItem> _SearchHistoryItemCommand;
        public DelegateCommand<SearchHistoryListItem> SearchHistoryItemCommand
        {
            get
            {
                return _SearchHistoryItemCommand
                    ?? (_SearchHistoryItemCommand = new DelegateCommand<SearchHistoryListItem>((item) =>
                    {
                        SearchText.Value = item.Keyword;
                    }
                    ));
            }
        }


        private DelegateCommand<Database.SearchHistory> _DeleteSearchHistoryItemCommand;
        public DelegateCommand<Database.SearchHistory> DeleteSearchHistoryItemCommand
        {
            get
            {
                return _DeleteSearchHistoryItemCommand
                    ?? (_DeleteSearchHistoryItemCommand = new DelegateCommand<Database.SearchHistory>((item) =>
                    {
                        Database.SearchHistoryDb.Remove(item.Keyword, item.Target);
                        var itemVM = SearchHistoryItems.FirstOrDefault(x => x.Keyword == item.Keyword && x.Target == item.Target);
                        if (itemVM != null)
                        {
                            SearchHistoryItems.Remove(itemVM);
                        }
                    }
                    ));
            }
        }
        /*
        public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			// コミュニティ検索はログインが必要            

			// ContentVM側のページタイトルが後で呼び出されるように、SearchPage側を先に呼び出す
			base.OnNavigatedTo(e, viewModelState);
        }

        public override void OnNavigatingFrom(NavigatingFromEventArgs e, Dictionary<string, object> viewModelState, bool suspending)
        {
            _LastSelectedTarget = SelectedTarget.Value;
            _LastKeyword = SearchText.Value;
            base.OnNavigatingFrom(e, viewModelState, suspending);
        }
        */
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
        public VideoSearchSource(VideoSearchOption searchOption, SearchProvider searchProvider, NGSettings ngSettings)
        {
            SearchOption = searchOption;
            SearchProvider = searchProvider;
            NgSettings = ngSettings;
        }

        public SearchProvider SearchProvider { get; }
        public NGSettings NgSettings { get; }
		public VideoSearchOption SearchOption { get; }


		

        protected override async IAsyncEnumerable<VideoInfoControlViewModel> GetPagedItemsImpl(int head, int count, [EnumeratorCancellation]CancellationToken cancellationToken)
        {
            VideoListingResponse res = null;
            if (SearchOption.SearchTarget == SearchTarget.Keyword)
            {
                res = await SearchProvider.GetKeywordSearch(SearchOption.Keyword, (uint)head, (uint)count, SearchOption.Sort, SearchOption.Order);
            }
            else if (SearchOption.SearchTarget == SearchTarget.Tag)
            {
                res = await SearchProvider.GetTagSearch(SearchOption.Keyword, (uint)head, (uint)count, SearchOption.Sort, SearchOption.Order);
            }


            if (res == null || res.VideoInfoItems == null)
            {
				yield break;
            }
            else
            {
                foreach (var item in res.VideoInfoItems.Where(x => x != null))
                {
					var vm = new VideoInfoControlViewModel(item.Video.Id);
					await vm.InitializeAsync(cancellationToken);
					yield return vm;
                }
            }
        }

        protected override async Task<int> ResetSourceImpl()
        {
            int totalCount = 0;
            if (SearchOption.SearchTarget == SearchTarget.Keyword)
            {
                var res = await SearchProvider.GetKeywordSearch(SearchOption.Keyword, 0, 2, SearchOption.Sort, SearchOption.Order);
                totalCount = (int)res.GetTotalCount();

            }
            else if (SearchOption.SearchTarget == SearchTarget.Tag)
            {
                var res = await SearchProvider.GetTagSearch(SearchOption.Keyword, 0, 2, SearchOption.Sort, SearchOption.Order);
                totalCount = (int)res.GetTotalCount();
            }

            return totalCount;
        }
    }



	public class SearchHistoryListItem : Interfaces.ISearchHistory
    {
        public Database.SearchHistory SearchHistory { get; }
        public string Keyword { get; private set; }
		public SearchTarget Target { get; private set; }

        SearchPageViewModel SearchPageVM { get; }

        public SearchHistoryListItem(Database.SearchHistory source, SearchPageViewModel parentVM)
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
