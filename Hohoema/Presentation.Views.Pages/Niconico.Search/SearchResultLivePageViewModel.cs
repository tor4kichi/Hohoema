using Hohoema.Models.Helpers;
using Hohoema.Models.Domain.Niconico.Live;
using Hohoema.Models.Domain.Niconico.Search;
using Hohoema.Models.Domain.PageNavigation;
using Hohoema.Models.UseCase;
using Hohoema.Models.UseCase.NicoVideos;
using Hohoema.Models.UseCase.PageNavigation;
using I18NPortable;
using Prism.Commands;
using Prism.Navigation;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using NiconicoSession = Hohoema.Models.Domain.Niconico.NiconicoSession;
using NiconicoToolkit.Live;
using NiconicoToolkit.Live.Search;
using System.Collections.ObjectModel;
using Uno.Extensions;
using Hohoema.Presentation.ViewModels.Niconico.Live;
using Hohoema.Models.Domain.Pins;
using Microsoft.Toolkit.Collections;
using Mntone.Nico2.Live.ReservationsInDetail;

namespace Hohoema.Presentation.ViewModels.Pages.Niconico.Search
{
    public class SearchResultLivePageViewModel : HohoemaListingPageViewModelBase<LiveInfoListItemViewModel>, INavigatedAwareAsync, IPinablePage, ITitleUpdatablePage
    {
        HohoemaPin IPinablePage.GetPin()
        {
            var keyword = (ItemsView.Source as LiveSearchSource)?.Query?.Keyword ?? Keyword;
            return new HohoemaPin()
            {
                Label = keyword,
                PageType = HohoemaPageType.SearchResultLive,
                Parameter = $"keyword={System.Net.WebUtility.UrlEncode(keyword)}&target={SearchTarget.Niconama}"
            };
        }

        IObservable<string> ITitleUpdatablePage.GetTitleObservable()
        {
            return this.ObserveProperty(x => x.Keyword);
        }

        public SearchResultLivePageViewModel(
            ApplicationLayoutManager applicationLayoutManager,
            NiconicoSession niconicoSession,
            SearchProvider searchProvider,
            PageManager pageManager,
            HohoemaPlaylist hohoemaPlaylist,
            SearchHistoryRepository searchHistoryRepository,
            NicoLiveCacheRepository nicoLiveCacheRepository,
            OpenLiveContentCommand openLiveContentCommand
            )
        {
            ApplicationLayoutManager = applicationLayoutManager;
            NiconicoSession = niconicoSession;
            SearchProvider = searchProvider;
            PageManager = pageManager;
            HohoemaPlaylist = hohoemaPlaylist;
            _searchHistoryRepository = searchHistoryRepository;
            _nicoLiveCacheRepository = nicoLiveCacheRepository;
            OpenLiveContentCommand = openLiveContentCommand;

            SelectedSearchSort = new ReactiveProperty<LiveSearchPageSortOrder>(SortOptionItems[0], mode: ReactivePropertyMode.DistinctUntilChanged);
            SelectedLiveStatus = new ReactiveProperty<LiveStatus>(LiveStatusItems[0], mode: ReactivePropertyMode.DistinctUntilChanged);
            SelectedProviders = new ObservableCollection<ProviderType>();

            IsTagSearch = new ReactiveProperty<bool>(false);
            IsTimeshiftAvairable = new ReactiveProperty<bool>(false);
            IsHideMemberOnly = new ReactiveProperty<bool>(false);
            IsDisableGrouping = new ReactiveProperty<bool>(false);

            Observable.Merge(
                SelectedSearchSort.ToUnit(),
                SelectedLiveStatus.ToUnit(),
                SelectedProviders.CollectionChangedAsObservable().ToUnit()
                )
                .Subscribe(async _ =>
                {
                    if (_NowNavigatingTo) { return; }

                    var query = _query;

                    if (query != null
                        && query.Keyword == Keyword
                            && query.SortOrder == SelectedSearchSort.Value
                            && query.ProviderTypes?.Empty() == false && SelectedProviders.All(x => query.ProviderTypes.Contains(x))
                            && query.LiveStatus == SelectedLiveStatus.Value
                    )
                    {
                        return;
                    }

                    ResetList();
                })
                .AddTo(_CompositeDisposable);
        }

        static public IReadOnlyList<LiveStatus> LiveStatusItems { get; } = new[] { LiveStatus.Onair, LiveStatus.Reserved, LiveStatus.Past };
        static public IReadOnlyList<LiveSearchPageSortOrder> SortOptionItems { get; } = Enum.GetValues(typeof(LiveSearchPageSortOrder)).Cast<LiveSearchPageSortOrder>().ToArray();
        static public IReadOnlyList<ProviderType> ProvidersItems { get; } = new[] { ProviderType.Official, ProviderType.Channel, ProviderType.Community };

        public ReactiveProperty<LiveStatus> SelectedLiveStatus { get; private set; }
        public ReactiveProperty<LiveSearchPageSortOrder> SelectedSearchSort { get; private set; }
        public ObservableCollection<ProviderType> SelectedProviders { get; private set; }
        public ReactiveProperty<bool> IsTagSearch { get; private set; }
        public ReactiveProperty<bool> IsTimeshiftAvairable { get; private set; }
        public ReactiveProperty<bool> IsDisableGrouping { get; private set; }
        public ReactiveProperty<bool> IsHideMemberOnly { get; private set; }

        private readonly SearchHistoryRepository _searchHistoryRepository;
        private readonly NicoLiveCacheRepository _nicoLiveCacheRepository;

        bool _NowNavigatingTo = false;

        private string _keyword;
        public string Keyword
        {
            get { return _keyword; }
            set { SetProperty(ref _keyword, value); }
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
        public NiconicoSession NiconicoSession { get; }
        public SearchProvider SearchProvider { get; }
        public PageManager PageManager { get; }
        public HohoemaPlaylist HohoemaPlaylist { get; }
        public OpenLiveContentCommand OpenLiveContentCommand { get; }

        #endregion

        public override async Task OnNavigatedToAsync(INavigationParameters parameters)
        {
            var mode = parameters.GetNavigationMode();
            if (mode == NavigationMode.New)
            {
                Keyword = System.Net.WebUtility.UrlDecode(parameters.GetValue<string>("keyword"));
            }

            _NowNavigatingTo = true;
            try
            {
                if (NiconicoSession.IsLoggedIn)
                {
                    _reservation = await NiconicoSession.Context.Live.GetReservationsInDetailAsync();
                }
            }
            finally
            {
                _NowNavigatingTo = false;
            }

            //SelectedSearchSort.Value = SearchOption.Sort;
            //SelectedLiveStatus.Value = SearchOption.LiveStatus;
            //SelectedProviders.Clear();
            //SelectedProviders.AddRange(SearchOption.Providers);

            if (!string.IsNullOrWhiteSpace(Keyword))
            {
                _searchHistoryRepository.Searched(Keyword, SearchTarget.Niconama);
            }

            await base.OnNavigatedToAsync(parameters);
        }

        protected override void PostResetList()
        {
            base.PostResetList();
        }


        protected override (int, IIncrementalSource<LiveInfoListItemViewModel>) GenerateIncrementalSource()
		{
            var query = LiveSearchOptionsQuery.Create(Keyword, SelectedLiveStatus.Value);
            if (SelectedProviders.Empty() is false)
            {
                query.UseProviderTypes(SelectedProviders);
            }

            query.UseSortOrder(SelectedSearchSort.Value);
            if (IsTagSearch.Value is true)
            {
                query.UseIsTagSearch(true);
            }

            if (IsTimeshiftAvairable.Value is true)
            {
                query.UseTimeshiftIsAvailable(true);
            }

            if (IsDisableGrouping.Value is true)
            {
                query.UseDisableGrouping(true);
            }

            if (IsHideMemberOnly.Value is true)
            {
                query.UseHideMemberOnly(true);
            }
            _query = query;

            return (LiveSearchSource.OneTimeLoadCount, new LiveSearchSource(query, _reservation, SearchProvider, NiconicoSession, _nicoLiveCacheRepository));
		}

        LiveSearchOptionsQuery _query;
        DelegateCommand _SearchOptionsUpdatedCommand;
        private ReservationsInDetailResponse _reservation;

        public DelegateCommand SearchOptionsUpdatedCommand => _SearchOptionsUpdatedCommand ??= new DelegateCommand(UpdatedSearchOptions);


        public async void UpdatedSearchOptions()
        {
            var query = _query;
            if ((query?.IsTagSearch ?? false) == IsTagSearch.Value
                    && (query?.HideMemberOnly ?? false) == IsHideMemberOnly.Value
                    && (query?.DisableGrouping ?? false) == IsDisableGrouping.Value
                    && (query?.TimeshiftIsAvailable ?? false) == IsTimeshiftAvairable.Value
                    )
            {
                return;
            }

            ResetList();
        }
    }



	public class LiveSearchSource : IIncrementalSource<LiveInfoListItemViewModel>
	{
        public LiveSearchSource(
            LiveSearchOptionsQuery query,
            ReservationsInDetailResponse reservation,
            SearchProvider searchProvider,
            NiconicoSession niconicoSession,
            NicoLiveCacheRepository nicoLiveCacheRepository
            )
        {
            Query = query;
            _reservation = reservation;
            SearchProvider = searchProvider;
            NiconicoSession = niconicoSession;
            _nicoLiveCacheRepository = nicoLiveCacheRepository;
        }

        private HashSet<string> SearchedVideoIdsHash = new HashSet<string>();
        private ReservationsInDetailResponse _reservation;

        public LiveSearchOptionsQuery Query { get; }
        private readonly NicoLiveCacheRepository _nicoLiveCacheRepository;
        public SearchProvider SearchProvider { get; }
        public NiconicoSession NiconicoSession { get; }

		public const int OneTimeLoadCount = 40;

       		
        async Task<IEnumerable<LiveInfoListItemViewModel>> IIncrementalSource<LiveInfoListItemViewModel>.GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken ct)
		{
            Query.UsePage(pageIndex);

            var res = await SearchProvider.LiveSearchAsync(Query);

            ct.ThrowIfCancellationRequested();

            using (res.Data)
            {
                List<LiveInfoListItemViewModel> items = new();
                foreach (var item in res.Data.SearchResultItems)
                {
                    if (!SearchedVideoIdsHash.Contains(item.LiveId))
                    {
                        SearchedVideoIdsHash.Add(item.LiveId);
                        _nicoLiveCacheRepository.AddOrUpdate(new NicoLive()
                        {
                            LiveId = item.LiveId,
                            BroadcasterId = item.ProviderName,
                            Title = item.Title,
                        });

                        var liveInfoVM = new LiveInfoListItemViewModel(item.LiveId);
                        liveInfoVM.Setup(item);

                        var reserve = _reservation?.ReservedProgram.FirstOrDefault(reservation => item.LiveId == reservation.Id);
                        if (reserve != null)
                        {
                            liveInfoVM.SetReservation(reserve);
                        }

                        items.Add(liveInfoVM);
                    }
                    else
                    {
                        continue;
                    }

                    ct.ThrowIfCancellationRequested();
                }

                return items;
            }
        }
    }
}
