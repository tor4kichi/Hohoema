using Hohoema.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hohoema.Models.Helpers;
using Mntone.Nico2.Searches.Live;
using Prism.Commands;
using Mntone.Nico2;
using Reactive.Bindings;
using System.Reactive.Linq;
using Reactive.Bindings.Extensions;
using Hohoema.ViewModels.Pages;
using Hohoema.Models.Provider;
using Unity;
using Hohoema.Services;
using Prism.Navigation;
using Hohoema.UseCase.Playlist;
using Hohoema.Interfaces;
using Hohoema.UseCase;
using I18NPortable;
using Hohoema.Models.Live;
using System.Threading;
using System.Runtime.CompilerServices;
using Hohoema.ViewModels.Player.Commands;
using Hohoema.Models.Repository.Niconico;

namespace Hohoema.ViewModels
{
    public class SearchResultLivePageViewModel : HohoemaListingPageViewModelBase<LiveInfoListItemViewModel>, INavigatedAwareAsync, IPinablePage, ITitleUpdatablePage
    {
        HohoemaPin IPinablePage.GetPin()
        {
            return new HohoemaPin()
            {
                Label = SearchOption.Keyword,
                PageType = HohoemaPageType.SearchResultLive,
                Parameter = $"keyword={System.Net.WebUtility.UrlEncode(SearchOption.Keyword)}&target={SearchOption.SearchTarget}"
            };
        }

        IObservable<string> ITitleUpdatablePage.GetTitleObservable()
        {
            return this.ObserveProperty(x => x.Keyword);
        }

        public SearchResultLivePageViewModel(
            ApplicationLayoutManager applicationLayoutManager,
            Models.NiconicoSession niconicoSession,
            SearchProvider searchProvider,
            PageManager pageManager,
            HohoemaPlaylist hohoemaPlaylist,
            OpenLiveContentCommand openLiveContentCommand
            )
        {
            SelectedSearchSort = new ReactiveProperty<LiveSearchSortOptionListItem>(LiveSearchSortOptionListItems[0], mode: ReactivePropertyMode.DistinctUntilChanged);
            SelectedLiveStatus = new ReactiveProperty<LiveSearchModeLiveStatusFilterOptionListItem>(LiveSearchLiveStatusListItems[0], mode: ReactivePropertyMode.DistinctUntilChanged);
            SelectedProvider = new ReactiveProperty<LiveSearchProviderOptionListItem>(LiveSearchProviderOptionListItems[0], mode: ReactivePropertyMode.DistinctUntilChanged);

            SelectedSearchTarget = new ReactiveProperty<SearchTarget>();

            IsTagSearch = new ReactiveProperty<bool>(false);
            IsExcludeCommunityMemberOnly = new ReactiveProperty<bool>(false);


            Observable.Merge(
                SelectedSearchSort.ToUnit(),
                SelectedLiveStatus.ToUnit(),
                SelectedProvider.ToUnit()
                )
                .Subscribe(async _ =>
                {
                    if (_NowNavigatingTo) { return; }

                    var selected = SelectedSearchSort.Value;
                    if (SearchOption.Sort == selected.SortType
                        && SearchOption.Provider == SelectedProvider.Value?.Provider
                        && SearchOption.LiveStatus == SelectedLiveStatus.Value.LiveStatus
                    )
                    {
                        return;
                    }

                    SearchOption.Provider = SelectedProvider.Value?.Provider;
                    SearchOption.Sort = SelectedSearchSort.Value.SortType;
                    SearchOption.LiveStatus = SelectedLiveStatus.Value.LiveStatus;
                    SearchOption.IsTagSearch = IsTagSearch.Value;
                    SearchOption.IsExcludeCommunityMemberOnly = IsExcludeCommunityMemberOnly.Value;

                    await ResetList();
                })
                .AddTo(_CompositeDisposable);
            
            ApplicationLayoutManager = applicationLayoutManager;
            NiconicoSession = niconicoSession;
            SearchProvider = searchProvider;
            PageManager = pageManager;
            HohoemaPlaylist = hohoemaPlaylist;
            OpenLiveContentCommand = openLiveContentCommand;
        }


        public class LiveSearchSortOptionListItem
        {
            public string Label { get; set; }
            public LiveSearchSortType SortType { get; set; }
        }

        public class LiveSearchModeLiveStatusFilterOptionListItem
        {
            public string Label { get; set; }
            public StatusType LiveStatus { get; set; }
        }

        public class LiveSearchProviderOptionListItem
        {
            public string Label { get; set; }
            public CommunityType Provider { get; set; }
        }

        static public IReadOnlyList<LiveSearchSortOptionListItem> LiveSearchSortOptionListItems { get; private set; }
        static public IReadOnlyList<LiveSearchModeLiveStatusFilterOptionListItem> LiveSearchLiveStatusListItems { get; private set; }
        static public IReadOnlyList<LiveSearchProviderOptionListItem> LiveSearchProviderOptionListItems { get; private set; }

        static SearchResultLivePageViewModel()
        {
            LiveSearchSortOptionListItems = new List<LiveSearchSortOptionListItem>()
            {
                new LiveSearchSortOptionListItem()
                {
                    SortType = LiveSearchSortType.StartTime | LiveSearchSortType.SortDecsending,
                    Label = "LiveSearchSortType.StartTime_Descending".Translate()
                },
                new LiveSearchSortOptionListItem()
                {
                    SortType = LiveSearchSortType.StartTime | LiveSearchSortType.SortAcsending,
                    Label = "LiveSearchSortType.StartTime_Ascending".Translate()
                },
                new LiveSearchSortOptionListItem()
                {
                    SortType = LiveSearchSortType.ScoreTimeshiftReserved | LiveSearchSortType.SortDecsending,
                    Label = "LiveSearchSortType.ScoreTimeshiftReserved_Descending".Translate()
                },
                new LiveSearchSortOptionListItem()
                {
                    SortType = LiveSearchSortType.ScoreTimeshiftReserved | LiveSearchSortType.SortAcsending,
                    Label = "LiveSearchSortType.ScoreTimeshiftReserved_Ascending".Translate()
                },
                new LiveSearchSortOptionListItem()
                {
                    SortType = LiveSearchSortType.ViewCounter | LiveSearchSortType.SortDecsending,
                    Label = "LiveSearchSortType.ViewCounter_Descending".Translate()
                },
                new LiveSearchSortOptionListItem()
                {
                    SortType = LiveSearchSortType.ViewCounter | LiveSearchSortType.SortAcsending,
                    Label = "LiveSearchSortType.ViewCounter_Ascending".Translate()
                },
                new LiveSearchSortOptionListItem()
                {
                    SortType = LiveSearchSortType.CommentCounter | LiveSearchSortType.SortDecsending,
                    Label = "LiveSearchSortType.CommentCounter_Descending".Translate()
                },
                new LiveSearchSortOptionListItem()
                {
                    SortType = LiveSearchSortType.CommentCounter | LiveSearchSortType.SortAcsending,
                    Label = "LiveSearchSortType.CommentCounter_Ascending".Translate()
                },
            };

            LiveSearchLiveStatusListItems = new List<LiveSearchModeLiveStatusFilterOptionListItem>()
            {
                new LiveSearchModeLiveStatusFilterOptionListItem()
                {
                    Label = "NicoliveSearchMode.OnAir".Translate(),
                    LiveStatus = StatusType.OnAir
                },
				new LiveSearchModeLiveStatusFilterOptionListItem()
				{
                    Label = "NicoliveSearchMode.Reserved".Translate(),
                    LiveStatus = StatusType.ComingSoon
                },
                new LiveSearchModeLiveStatusFilterOptionListItem()
                {
                    Label = "NicoliveSearchMode.Closed".Translate(),
                    LiveStatus = StatusType.Closed
                },
            };


            LiveSearchProviderOptionListItems = new List<LiveSearchProviderOptionListItem>()
            {
                new LiveSearchProviderOptionListItem()
                {
                    Label = Mntone.Nico2.Live.CommunityType.Official.Translate(),
                    Provider = Mntone.Nico2.Live.CommunityType.Official,
                },
                new LiveSearchProviderOptionListItem()
                {
                    Label = Mntone.Nico2.Live.CommunityType.Channel.Translate(),
                    Provider = Mntone.Nico2.Live.CommunityType.Channel,
                },
                new LiveSearchProviderOptionListItem()
                {
                    Label = Mntone.Nico2.Live.CommunityType.Community.Translate(),
                    Provider = Mntone.Nico2.Live.CommunityType.Community,
                },
            };
        }

        public ReactiveProperty<LiveSearchSortOptionListItem> SelectedSearchSort { get; private set; }
        public ReactiveProperty<LiveSearchModeLiveStatusFilterOptionListItem> SelectedLiveStatus { get; private set; }
        public ReactiveProperty<LiveSearchProviderOptionListItem> SelectedProvider { get; private set; }
        public ReactiveProperty<bool> IsTagSearch { get; private set; }
        public ReactiveProperty<bool> IsExcludeCommunityMemberOnly { get; private set; }


        static public LiveSearchPagePayloadContent SearchOption { get; private set; }

        private string _keyword;
        public string Keyword
        {
            get { return _keyword; }
            set { SetProperty(ref _keyword, value); }
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
                            PageManager.Search(target.Value, SearchOption.Keyword);
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

        public ApplicationLayoutManager ApplicationLayoutManager { get; }
        public Models.NiconicoSession NiconicoSession { get; }
        public SearchProvider SearchProvider { get; }
        public PageManager PageManager { get; }
        public HohoemaPlaylist HohoemaPlaylist { get; }
        public OpenLiveContentCommand OpenLiveContentCommand { get; }

        #endregion

        public override Task OnNavigatedToAsync(INavigationParameters parameters)
        {
            var mode = parameters.GetNavigationMode();
            if (mode == NavigationMode.New)
            {
                Keyword = System.Net.WebUtility.UrlDecode(parameters.GetValue<string>("keyword"));

                SearchOption = new LiveSearchPagePayloadContent()
                {
                    Keyword = Keyword
                };
            }

            SelectedSearchTarget.Value = SearchTarget.Niconama;

            _NowNavigatingTo = true;
            SelectedSearchSort.Value = LiveSearchSortOptionListItems.FirstOrDefault(x => x.SortType == SearchOption.Sort);
            SelectedLiveStatus.Value = LiveSearchLiveStatusListItems.FirstOrDefault(x => x.LiveStatus == SearchOption.LiveStatus) ?? LiveSearchLiveStatusListItems.First();
            SelectedProvider.Value = LiveSearchProviderOptionListItems.FirstOrDefault(x => x.Provider == SearchOption.Provider);
            _NowNavigatingTo = false;


            Database.SearchHistoryDb.Searched(SearchOption.Keyword, SearchOption.SearchTarget);

            return base.OnNavigatedToAsync(parameters);
        }

        bool _NowNavigatingTo = false;

		protected override void PostResetList()
        {
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

		public uint OneTimeLoadCount => 10;

        public List<LiveSearchResultItem> Info { get; } = new List<LiveSearchResultItem>();


        Mntone.Nico2.Live.ReservationsInDetail.ReservationsInDetailResponse _Reservations;

        


        public HashSet<string> SearchedVideoIdsHash = new HashSet<string>();

		private Task<LiveSearchResponse> GetLiveSearchResponseOnCurrentOption(int from, int length)
		{
            var liveStatus = SearchOption.LiveStatus switch
            {
                StatusType.OnAir => SearchFilterField.LiveStatusOnAir,
                StatusType.Closed => SearchFilterField.LiveStatusPast,
                StatusType.ComingSoon => SearchFilterField.LiveStatusReserved,
                _ => throw new NotSupportedException(SearchOption.LiveStatus.ToString())
            };

            var provider = SearchOption.Provider switch
            {
                CommunityType.Community => SearchFilterField.ProviderTypeCommunity,
                CommunityType.Channel => SearchFilterField.ProviderTypeChannel,
                CommunityType.Official => SearchFilterField.ProviderTypeOfficial,
                _ => string.Empty
            };

            if (!string.IsNullOrEmpty(provider))
            {
                return SearchProvider.LiveSearchAsync(
                        SearchOption.Keyword,
                        offset: from,
                        limit: length,
                        sortType: SearchOption.Sort,
                        filterExpression: x => x.LiveStatus == liveStatus && x.ProviderType == provider
                        );
            }
            else 
            {
                return SearchProvider.LiveSearchAsync(
                        SearchOption.Keyword,
                        offset: from,
                        limit: length,
                        sortType: SearchOption.Sort,
                        filterExpression: x => x.LiveStatus == liveStatus
                        );
            }
        }

		public async Task<int> ResetSource()
		{
			int totalCount = 0;
			try
			{
				var res = await GetLiveSearchResponseOnCurrentOption(0, 30);

                if (res.IsOK)
                {
                    foreach (var v in res.Data)
                    {
                        AddLive(v);
                    }
                }

                totalCount = res.Meta.TotalCount ?? 0;
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

        private void AddLive(LiveSearchResultItem live)
        {
            if (!SearchedVideoIdsHash.Contains(live.ContentId))
            {
                Info.Add(live);
                SearchedVideoIdsHash.Add(live.ContentId);
            }
        }
		public async IAsyncEnumerable<LiveInfoListItemViewModel> GetPagedItems(int head, int count, [EnumeratorCancellation] CancellationToken cancellationToken)
		{
            if (Info.Count < (head + count))
            {
                var res = await GetLiveSearchResponseOnCurrentOption(Info.Count, count);
                
                if (res.IsOK)
                {
                    foreach (var v in res.Data)
                    {
                        AddLive(v);
                    }
                }
            }

            cancellationToken.ThrowIfCancellationRequested();

            foreach (var item in Info.Skip(head).Take(count).Select(x =>
			{
                var liveInfoVM = new LiveInfoListItemViewModel(x.ContentId);
                liveInfoVM.Setup(x);

                var reserve = _Reservations?.ReservedProgram.FirstOrDefault(reservation => x.ContentId == reservation.Id);
                if (reserve != null)
                {
                    liveInfoVM.SetReservation(reserve);
                }

                return liveInfoVM;
			}))
            {
                yield return item;
            }
		}
	}
}
