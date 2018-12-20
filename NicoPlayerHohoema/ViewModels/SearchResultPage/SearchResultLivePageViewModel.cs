using NicoPlayerHohoema.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Prism.Windows.Navigation;
using NicoPlayerHohoema.Models.Helpers;
using Mntone.Nico2.Searches.Live;
using Prism.Commands;
using Mntone.Nico2;
using Reactive.Bindings;
using System.Reactive.Linq;
using Reactive.Bindings.Extensions;
using System.Collections.Async;
using Windows.UI.Xaml.Navigation;
using NicoPlayerHohoema.Services.Page;
using NicoPlayerHohoema.Models.Provider;
using Microsoft.Practices.Unity;
using NicoPlayerHohoema.Services;

namespace NicoPlayerHohoema.ViewModels
{
    public class SearchResultLivePageViewModel : HohoemaListingPageViewModelBase<LiveInfoListItemViewModel>
	{
        public SearchResultLivePageViewModel(
            Models.NiconicoSession niconicoSession,
            SearchProvider searchProvider,
            Services.PageManager pageManager,
            HohoemaPlaylist hohoemaPlaylist
            )
            : base(pageManager, useDefaultPageTitle: false)
        {
            SelectedSearchSort = new ReactiveProperty<LiveSearchSortOptionListItem>(LiveSearchSortOptionListItems[0], mode: ReactivePropertyMode.DistinctUntilChanged);
            SelectedSearchMode = new ReactiveProperty<LiveSearchModeOptionListItem>(LiveSearchModeOptionListItems[0], mode: ReactivePropertyMode.DistinctUntilChanged);
            SelectedProvider = new ReactiveProperty<LiveSearchProviderOptionListItem>(LiveSearchProviderOptionListItems[0], mode: ReactivePropertyMode.DistinctUntilChanged);

            SelectedSearchTarget = new ReactiveProperty<SearchTarget>();

            Observable.Merge(
                SelectedSearchSort.ToUnit(),
                SelectedSearchMode.ToUnit(),
                SelectedProvider.ToUnit()
                )
                .Subscribe(async _ =>
                {
                    if (_NowNavigatingTo) { return; }

                    var selected = SelectedSearchSort.Value;
                    if (SearchOption.Order == selected.Order
                        && SearchOption.Sort == selected.Sort
                        && SearchOption.Mode == SelectedSearchMode.Value?.Mode
                        && SearchOption.Provider == SelectedProvider.Value?.Provider
                    )
                    {
                        return;
                    }

                    SearchOption.Mode = SelectedSearchMode.Value.Mode;
                    SearchOption.Provider = SelectedProvider.Value.Provider;
                    SearchOption.Sort = SelectedSearchSort.Value.Sort;
                    SearchOption.Order = SelectedSearchSort.Value.Order;

                    await ResetList();
                })
                .AddTo(_CompositeDisposable);
            NiconicoSession = niconicoSession;
            SearchProvider = searchProvider;
            HohoemaPlaylist = hohoemaPlaylist;
        }


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

        static public IReadOnlyList<LiveSearchSortOptionListItem> LiveSearchSortOptionListItems { get; private set; }
        static public IReadOnlyList<LiveSearchModeOptionListItem> LiveSearchModeOptionListItems { get; private set; }
        static public IReadOnlyList<LiveSearchProviderOptionListItem> LiveSearchProviderOptionListItems { get; private set; }

        static SearchResultLivePageViewModel()
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
                item.Label = Services.Helpers.SortHelper.ToCulturizedText(item.Sort, item.Order);
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


        public LiveSearchPagePayloadContent SearchOption { get; private set; }



        private string _SearchOptionText;
        public string SearchOptionText
        {
            get { return _SearchOptionText; }
            set { SetProperty(ref _SearchOptionText, value); }
        }


        public static List<SearchTarget> SearchTargets { get; } = Enum.GetValues(typeof(SearchTarget)).Cast<SearchTarget>().ToList();

        public ReactiveProperty<SearchTarget> SelectedSearchTarget { get; }

        private DelegateCommand<SearchTarget?> _ChangeSearchTargetCommand;
        public DelegateCommand<SearchTarget?> ChangeSearchTargetCommand
        {
            get
            {
                return _ChangeSearchTargetCommand
                    ?? (_ChangeSearchTargetCommand = new DelegateCommand<SearchTarget?>(target =>
                    {
                        if (target.HasValue && target.Value != SearchOption.SearchTarget)
                        {
                            var payload = SearchPagePayloadContentHelper.CreateDefault(target.Value, SearchOption.Keyword);
                            PageManager.Search(payload);
                        }
                    }));
            }
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

        public Models.NiconicoSession NiconicoSession { get; }
        public SearchProvider SearchProvider { get; }
        public Services.HohoemaPlaylist HohoemaPlaylist { get; }

        #endregion

        protected override string ResolvePageName()
        {
            return $"\"{SearchOption.Keyword}\"";
        }

        bool _NowNavigatingTo = false;

		public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
        {
            if (e.Parameter is string && e.NavigationMode == NavigationMode.New)
            {
                SearchOption = PagePayloadBase.FromParameterString<LiveSearchPagePayloadContent>(e.Parameter as string);
            }

            SelectedSearchTarget.Value = SearchOption?.SearchTarget ?? SearchTarget.Niconama;

            if (SearchOption == null)
            {
                var oldOption = viewModelState[nameof(SearchOption)] as string;
                SearchOption = PagePayloadBase.FromParameterString<LiveSearchPagePayloadContent>(oldOption);

                if (SearchOption == null)
                {
                    throw new Exception();
                }
            }

            _NowNavigatingTo = true;
            SelectedSearchSort.Value = LiveSearchSortOptionListItems.FirstOrDefault(x => x.Sort == SearchOption.Sort && x.Order == SearchOption.Order);
            SelectedSearchMode.Value = LiveSearchModeOptionListItems.FirstOrDefault(x => x.Mode == SearchOption.Mode) ?? LiveSearchModeOptionListItems.First();
            SelectedProvider.Value = LiveSearchProviderOptionListItems.FirstOrDefault(x => x.Provider == SearchOption.Provider);
            _NowNavigatingTo = false;


            Database.SearchHistoryDb.Searched(SearchOption.Keyword, SearchOption.SearchTarget);


            base.OnNavigatedTo(e, viewModelState);
        }

        public override void OnNavigatingFrom(NavigatingFromEventArgs e, Dictionary<string, object> viewModelState, bool suspending)
        {
            viewModelState[nameof(SearchOption)] = SearchOption.ToParameterString();

            base.OnNavigatingFrom(e, viewModelState, suspending);
        }

        private void ResetSearchOptionText()
        {
            var optionText = Services.Helpers.SortHelper.ToCulturizedText(SearchOption.Sort, SearchOption.Order);
            var providerText = SelectedProvider.Value.Label;
            string mode = "";
            if (SearchOption.Mode.HasValue)
            {
                switch (SearchOption.Mode)
                {
                    case Mntone.Nico2.Searches.Live.NicoliveSearchMode.OnAir:
                        mode = "放送中";
                        break;
                    case Mntone.Nico2.Searches.Live.NicoliveSearchMode.Reserved:
                        mode = "放送予定";
                        break;
                    case Mntone.Nico2.Searches.Live.NicoliveSearchMode.Closed:
                        mode = "放送終了";
                        break;
                    default:
                        break;
                }
            }
            else
            {
                mode = "すべて";
            }

            SearchOptionText = $"{optionText}/{mode}/{providerText}";
        }

        protected override void PostResetList()
        {
            ResetSearchOptionText();

            base.PostResetList();
        }


        protected override IIncrementalSource<LiveInfoListItemViewModel> GenerateIncrementalSource()
		{
			return new LiveSearchSource(SearchOption, SearchProvider, NiconicoSession);
		}
	}



	public class LiveSearchSource : IIncrementalSource<LiveInfoListItemViewModel>
	{
        public LiveSearchSource(
            LiveSearchPagePayloadContent searchOption,
            SearchProvider searchProvider,
            Models.NiconicoSession niconicoSession
            )
        {
            SearchOption = searchOption;
            SearchProvider = searchProvider;
            NiconicoSession = niconicoSession;
        }

        public LiveSearchPagePayloadContent SearchOption { get; private set; }
        public SearchProvider SearchProvider { get; }
        public Models.NiconicoSession NiconicoSession { get; }
        public List<Tag> Tags { get; private set; }

		public uint OneTimeLoadCount => 10;

        public List<VideoInfo> Info { get; } = new List<VideoInfo>();


        Mntone.Nico2.Live.ReservationsInDetail.ReservationsInDetailResponse _Reservations;

        


        public HashSet<string> SearchedVideoIdsHash = new HashSet<string>();

		private Task<NicoliveVideoResponse> GetLiveSearchResponseOnCurrentOption(uint from, uint length)
		{
			return SearchProvider.LiveSearchAsync(
					SearchOption.Keyword,
					SearchOption.IsTagSearch,
					from: from,
					length: length,
					provider: SearchOption.Provider,
					sort: SearchOption.Sort,
					order: SearchOption.Order,
					mode: SearchOption.Mode
					);
		}

		public async Task<int> ResetSource()
		{
			int totalCount = 0;
			try
			{
				var res = await GetLiveSearchResponseOnCurrentOption(0, 30);

                if (res.VideoInfo != null)
                {
                    foreach (var v in res.VideoInfo)
                    {
                        AddLive(v);
                    }
                }

                Tags = res.Tags?.Tag.ToList();
                totalCount = res.TotalCount.FilteredCount;
			}
			catch { }

            try
            {
                // ログインしてない場合はタイムシフト予約は取れない
                if(NiconicoSession.IsLoggedIn)
                {
                    _Reservations = await NiconicoSession.Context.Live.GetReservationsInDetailAsync();
                }
            }
            catch { }

            return totalCount;
		}

        private void AddLive(VideoInfo live)
        {
            if (!SearchedVideoIdsHash.Contains(live.Video.Id))
            {
                Info.Add(live);
                SearchedVideoIdsHash.Add(live.Video.Id);
            }
        }
		public async Task<IAsyncEnumerable<LiveInfoListItemViewModel>> GetPagedItems(int head, int count)
		{
            if (Info.Count < (head + count))
            {
                var res = await GetLiveSearchResponseOnCurrentOption((uint)Info.Count, (uint)count);
                Tags = res.Tags?.Tag.ToList();

                if (res.VideoInfo != null)
                {
                    foreach (var v in res.VideoInfo)
                    {
                        AddLive(v);
                    }
                }
            }

            return Info.Skip(head).Take(count).Select(x =>
			{
                var liveInfoVM = App.Current.Container.Resolve<LiveInfoListItemViewModel>();
                liveInfoVM.Setup(x);

                var reserve = _Reservations?.ReservedProgram.FirstOrDefault(reservation => x.Video.Id == reservation.Id);
                if (reserve != null)
                {
                    liveInfoVM.SetReservation(reserve);
                }

                return liveInfoVM;
			})
            .ToAsyncEnumerable();
		}
	}
}
