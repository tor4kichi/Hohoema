using Prism.Windows.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Windows.Navigation;
using NicoPlayerHohoema.Models;
using System.Collections.ObjectModel;
using NicoPlayerHohoema.Helpers;
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
	public class SearchPageViewModel : HohoemaListingPageViewModelBase<SearchHistoryListItem>
    {
		public ISearchPagePayloadContent RequireSearchOption { get; private set; }


		public ReactiveCommand DoSearchCommand { get; private set; }

		public ReactiveProperty<string> SearchText { get; private set; }
		public List<SearchTarget> TargetListItems { get; private set; }
		public ReactiveProperty<SearchTarget> SelectedTarget { get; private set; }
		
		public Dictionary<SearchTarget, SearchOptionViewModelBase> SearchOptionVMDict { get; private set; }
		public ReactiveProperty<SearchOptionViewModelBase> SearchOptionVM { get; private set; }

		public bool IsSearchKeyword => RequireSearchOption is KeywordSearchPagePayloadContent;

		public bool IsSearchTag => RequireSearchOption is TagSearchPagePayloadContent;

		public bool IsSearchMylist => RequireSearchOption is MylistSearchPagePayloadContent;

		public bool IsSearchCommunity => RequireSearchOption is CommunitySearchPagePayloadContent;

		public bool IsSearchNiconama => RequireSearchOption is LiveSearchPagePayloadContent;

		private void RaiseSearchTargetFlags()
		{
			RaisePropertyChanged(nameof(IsSearchKeyword));
			RaisePropertyChanged(nameof(IsSearchTag));
			RaisePropertyChanged(nameof(IsSearchMylist));
			RaisePropertyChanged(nameof(IsSearchCommunity));
			RaisePropertyChanged(nameof(IsSearchNiconama));
		}

		public SearchPageViewModel(HohoemaApp hohoemaApp, PageManager pageManager)
			: base(hohoemaApp, pageManager)
		{
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
                PageManager.Search(searchOption);
			})
			.AddTo(_CompositeDisposable);
		}

        protected override IIncrementalSource<SearchHistoryListItem> GenerateIncrementalSource()
        {
            return new SearchHistoryIncrementalLoadingSource(HohoemaApp, this);
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
                    ?? (_DeleteAllSearchHistoryCommand = new DelegateCommand(async () =>
                    {
                        SearchHistoryDb.Clear();

                        await ResetList();
                    },
                    () => SearchHistoryDb.GetHistoryCount() > 0
                    ));
            }
        }


        private DelegateCommand _DeleteSelectedSearchHistoryCommand;
        public DelegateCommand DeleteSelectedSearchHistoryCommand
        {
            get
            {
                return _DeleteSelectedSearchHistoryCommand
                    ?? (_DeleteSelectedSearchHistoryCommand = new DelegateCommand(async () =>
                    {
                        foreach (var item in SelectedItems)
                        {
                            SearchHistoryDb.RemoveHistory(item.Keyword, item.Target);
                        }

                        await ResetList();
                    }
                    , () => SelectedItems.Count > 0
                    ));
            }
        }

        internal void OnSearchHistorySelected(SearchHistory item)
		{
			SearchText.Value = item.Keyword;
			SelectedTarget.Value = TargetListItems.Single(x => x == item.Target);

			DoSearchCommand.Execute();
		}

		public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			// コミュニティ検索はログインが必要
            if (HohoemaApp.IsLoggedIn)
            {
                ChangeRequireServiceLevel(HohoemaAppServiceLevel.LoggedIn);
            }
            else
            {
                ChangeRequireServiceLevel(HohoemaAppServiceLevel.OnlineWithoutLoggedIn);
            }

			// ContentVM側のページタイトルが後で呼び出されるように、SearchPage側を先に呼び出す
			base.OnNavigatedTo(e, viewModelState);

            SelectedItems.CollectionChangedAsObservable()
                .Subscribe(_ =>
                {
                    DeleteSelectedSearchHistoryCommand.RaiseCanExecuteChanged();
                });
        }

        protected override async Task NavigatedToAsync(CancellationToken cancelToken, NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
        {
            await ResetList();

            await base.NavigatedToAsync(cancelToken, e, viewModelState);
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
				item.Label = Helpers.SortHelper.ToCulturizedText(item.Sort, item.Order);
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

		public class LiveSearchProviderOptionListItem
		{
			public string Label { get; set; }
			public Mntone.Nico2.Live.CommunityType? Provider { get; set; }
		}

		public static IReadOnlyList<LiveSearchSortOptionListItem> LiveSearchSortOptionListItems { get; private set; }
		public static IReadOnlyList<LiveSearchModeOptionListItem> LiveSearchModeOptionListItems { get; private set; }
		public static IReadOnlyList<LiveSearchProviderOptionListItem> LiveSearchProviderOptionListItems { get; private set; }

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
                        Order = Order.Ascending,
                    },
                    new LiveSearchSortOptionListItem()
                    {
                        Sort = x,
                        Order = Order.Descending,
                    },
                };
			})
			.ToList();

			foreach (var item in LiveSearchSortOptionListItems)
			{
				item.Label = Helpers.SortHelper.ToCulturizedText(item.Sort, item.Order);
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
                /*
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
                */
			};


			LiveSearchProviderOptionListItems = new List<LiveSearchProviderOptionListItem>()
			{
				new LiveSearchProviderOptionListItem()
				{
					Label = "すべて",
					Provider = null,
				},

				new LiveSearchProviderOptionListItem()
				{
					Label = "公式",
					Provider = Mntone.Nico2.Live.CommunityType.Official,
				},
				new LiveSearchProviderOptionListItem()
				{
					Label = "チャンネル",
					Provider = Mntone.Nico2.Live.CommunityType.Channel,
				},
				new LiveSearchProviderOptionListItem()
				{
					Label = "ユーザー",
					Provider = Mntone.Nico2.Live.CommunityType.Community,
				},
							
			};
		}

		public ReactiveProperty<LiveSearchSortOptionListItem> SelectedSearchSort { get; private set; }
		public ReactiveProperty<LiveSearchModeOptionListItem> SelectedSearchMode { get; private set; }
		public ReactiveProperty<bool> IsTagSearch { get; private set; }
		public ReactiveProperty<LiveSearchProviderOptionListItem> SelectedProvider { get; private set; }

		public LiveSearchOptionViewModel()
		{
			SelectedSearchSort = new ReactiveProperty<LiveSearchSortOptionListItem>(LiveSearchSortOptionListItems[0]);
			SelectedSearchMode = new ReactiveProperty<LiveSearchModeOptionListItem>(LiveSearchModeOptionListItems[0]);
			SelectedProvider = new ReactiveProperty<LiveSearchProviderOptionListItem>(LiveSearchProviderOptionListItems[0]);
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
				Provider = SelectedProvider.Value.Provider,
				IsTagSearch = IsTagSearch.Value
			};
		}
	}




	public class VideoSearchSource : HohoemaIncrementalSourceBase<VideoInfoControlViewModel>
	{
		public int MaxPageCount { get; private set; }

		HohoemaApp _HohoemaApp;
		PageManager _PageManager;
		public VideoSearchOption SearchOption { get; private set; }

		public VideoSearchSource(VideoSearchOption searchOption, HohoemaApp hohoemaApp, PageManager pageManager)
		{
			_HohoemaApp = hohoemaApp;
			_PageManager = pageManager;
			SearchOption = searchOption;
		}

		VideoListingResponse res;

		

        protected override async Task<IAsyncEnumerable<VideoInfoControlViewModel>> GetPagedItemsImpl(int head, int count)
        {
            if (SearchOption.SearchTarget == SearchTarget.Keyword)
            {
                res = await _HohoemaApp.ContentProvider.GetKeywordSearch(SearchOption.Keyword, (uint)head, (uint)count, SearchOption.Sort, SearchOption.Order);
            }
            else if (SearchOption.SearchTarget == SearchTarget.Tag)
            {
                res = await _HohoemaApp.ContentProvider.GetTagSearch(SearchOption.Keyword, (uint)head, (uint)count, SearchOption.Sort, SearchOption.Order);
            }


            if (res == null && res.VideoInfoItems == null)
            {
                return AsyncEnumerable.Empty<VideoInfoControlViewModel>();
            }
            else
            {
                return res.VideoInfoItems.Select(item =>
                {
                    var vm = new VideoInfoControlViewModel(item.Video.Id);
                    vm.SetupDisplay(item);
                    return vm;
                })
                .ToAsyncEnumerable();
            }
        }

        protected override async Task<int> ResetSourceImpl()
        {
            int totalCount = 0;
            if (SearchOption.SearchTarget == SearchTarget.Keyword)
            {
                var res = await _HohoemaApp.ContentProvider.GetKeywordSearch(SearchOption.Keyword, 0, 2, SearchOption.Sort, SearchOption.Order);
                totalCount = (int)res.GetTotalCount();

            }
            else if (SearchOption.SearchTarget == SearchTarget.Tag)
            {
                var res = await _HohoemaApp.ContentProvider.GetTagSearch(SearchOption.Keyword, 0, 2, SearchOption.Sort, SearchOption.Order);
                totalCount = (int)res.GetTotalCount();
            }

            return totalCount;
        }
    }



	public class SearchHistoryListItem : SelectableItem<SearchHistory>, Interfaces.ISearchHistory
    {
		public string Keyword { get; private set; }
		public SearchTarget Target { get; private set; }



		public SearchHistoryListItem(SearchHistory source, Action<SearchHistory> selectedAction) : base(source, selectedAction)
		{
			Keyword = source.Keyword;
			Target = source.Target;
		}
	}

    public class SearchHistoryIncrementalLoadingSource : IIncrementalSource<SearchHistoryListItem>
    {
        private HohoemaApp _HohoemaApp;
        private SearchPageViewModel _SearchPageViewModel;
        public SearchHistoryIncrementalLoadingSource(HohoemaApp hohoemaApp, SearchPageViewModel parentPage)
        {
            _HohoemaApp = hohoemaApp;
            _SearchPageViewModel = parentPage;
        }

        public uint OneTimeLoadCount => 100;

        public Task<IAsyncEnumerable<SearchHistoryListItem>> GetPagedItems(int head, int count)
        {
            var items = SearchHistoryDb.GetHistoryItems().Skip(head).Take(count)
                .Select(x => new SearchHistoryListItem(x, _SearchPageViewModel.OnSearchHistorySelected))
                .ToArray();

            return Task.FromResult(items.ToAsyncEnumerable());
        }

        public Task<int> ResetSource()
        {
            return Task.FromResult(SearchHistoryDb.GetHistoryCount());
        }
    }
}
