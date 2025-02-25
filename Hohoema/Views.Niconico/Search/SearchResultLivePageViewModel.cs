#nullable enable
using CommunityToolkit.Mvvm.Input;
using Hohoema.Models.Niconico;
using Hohoema.Models.Niconico.Live;
using Hohoema.Models.Niconico.Search;
using Hohoema.Models.PageNavigation;
using Hohoema.Models.Pins;
using Hohoema.Services;
using Hohoema.ViewModels.Niconico.Live;
using I18NPortable;
using LiteDB;
using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.Collections;
using NiconicoToolkit.Live;
using NiconicoToolkit.Live.Timeshift;
using NiconicoToolkit.Search.Live;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Xaml.Navigation;
using NiconicoSession = Hohoema.Models.Niconico.NiconicoSession;
using LiveStatus = NiconicoToolkit.Search.Live.SearchLiveStatus;
using LiveSort = NiconicoToolkit.Search.Live.Sort;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.Messaging;

namespace Hohoema.ViewModels.Pages.Niconico.Search;

public sealed partial class SearchResultLivePageViewModel 
    : HohoemaListingPageViewModelBase<LiveInfoListItemViewModel>
    , IPinablePage
    , ITitleUpdatablePage
{
    HohoemaPin IPinablePage.GetPin()
    {
        var keyword = (ItemsView?.Source as LiveSearchSource)?.Keyword ?? Keyword;
        return new HohoemaPin()
        {
            Label = keyword,
            PageType = HohoemaPageType.Search,
            Parameter = $"keyword={System.Net.WebUtility.UrlEncode(keyword)}&target={SearchTarget.Niconama}"
        };
    }

    IObservable<string> ITitleUpdatablePage.GetTitleObservable()
    {
        return this.ObserveProperty(x => x.Keyword);
    }

    public SearchResultLivePageViewModel(
        IMessenger messenger,
        ILoggerFactory loggerFactory,
        ApplicationLayoutManager applicationLayoutManager,
        NiconicoSession niconicoSession,
        SearchProvider searchProvider,        
        SearchHistoryRepository searchHistoryRepository,
        NicoLiveCacheRepository nicoLiveCacheRepository,
        OpenLiveContentCommand openLiveContentCommand
        )
        : base(loggerFactory.CreateLogger<SearchResultLivePageViewModel>())
    {
        _messenger = messenger;
        ApplicationLayoutManager = applicationLayoutManager;
        NiconicoSession = niconicoSession;
        SearchProvider = searchProvider;
        _searchHistoryRepository = searchHistoryRepository;
        _nicoLiveCacheRepository = nicoLiveCacheRepository;
        OpenLiveContentCommand = openLiveContentCommand;

        SelectedSort = new ReactiveProperty<LiveSort>(LiveSort.RecentAsc, mode: ReactivePropertyMode.DistinctUntilChanged);
        SelectedStatus = new ReactiveProperty<LiveStatus>(LiveStatus.ON_AIR, mode: ReactivePropertyMode.DistinctUntilChanged);
        SelectedProvider = new ReactiveProperty<LiveProvider>(LiveProvider.COMMUNITY, mode: ReactivePropertyMode.DistinctUntilChanged);

        Observable.Merge(
            SelectedSort.ToUnit(),
            SelectedStatus.ToUnit(),
            SelectedProvider.ToUnit()
            )
            .Subscribe(_ =>
            {
                if (_NowNavigatingTo) { return; }

                ResetList();
            })
            .AddTo(_CompositeDisposable);
    }

    public IReadOnlyList<LiveStatus> LiveStatusItems { get; } = new[] { LiveStatus.ON_AIR, LiveStatus.RELEASED, LiveStatus.ENDED };
    public IReadOnlyList<LiveSort> SortOptionItems { get; } = Enum.GetValues(typeof(LiveSort)).Cast<LiveSort>().ToArray();
    public IReadOnlyList<LiveProvider> ProvidersItems { get; } = new[] { LiveProvider.COMMUNITY, LiveProvider.CHANNEL, LiveProvider.OFFICIAL };
    
    public ReactiveProperty<LiveStatus> SelectedStatus { get; private set; }
    public ReactiveProperty<LiveSort> SelectedSort { get; private set; }
    public ReactiveProperty<LiveProvider> SelectedProvider { get; private set; }

    public ApplicationLayoutManager ApplicationLayoutManager { get; }
    public NiconicoSession NiconicoSession { get; }
    public SearchProvider SearchProvider { get; }
    public OpenLiveContentCommand OpenLiveContentCommand { get; }

    private readonly IMessenger _messenger;
    private readonly SearchHistoryRepository _searchHistoryRepository;
    private readonly NicoLiveCacheRepository _nicoLiveCacheRepository;

    bool _NowNavigatingTo = false;

    private string? _keyword;
    public string? Keyword
    {
        get { return _keyword; }
        set { SetProperty(ref _keyword, value); }
    }

    #region Commands


    private RelayCommand _ShowSearchHistoryCommand;
    public RelayCommand ShowSearchHistoryCommand
		{
			get
			{
				return _ShowSearchHistoryCommand
					?? (_ShowSearchHistoryCommand = new RelayCommand(() =>
					{
                        _ = _messenger.OpenPageAsync(HohoemaPageType.Search);
					}));
			}
		}

    #endregion

    private TimeshiftReservationsResponse? _reservation;

    public override async Task OnNavigatedToAsync(INavigationParameters parameters)
    {
        var mode = parameters.GetNavigationMode();
        if (mode == NavigationMode.New)
        {
            Keyword = System.Net.WebUtility.UrlDecode(parameters.GetValue<string>("keyword"));
        }

        Title = $"{"Search".Translate()} '{Keyword}'";

        _NowNavigatingTo = true;
        try
        {
            _reservation = NiconicoSession.IsLoggedIn ? await NiconicoSession.ToolkitContext.Timeshift.GetTimeshiftReservationsAsync() : null;
        }
        catch
        {
            _reservation = null;
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
            _searchHistoryRepository.Searched(Keyword!, SearchTarget.Niconama);
        }

        await base.OnNavigatedToAsync(parameters);
    }

    protected override void PostResetList()
    {
        base.PostResetList();
    }


    protected override (int, IIncrementalSource<LiveInfoListItemViewModel>) GenerateIncrementalSource()
	{
        Guard.IsNotNullOrEmpty(Keyword, nameof(Keyword));

        return (LiveSearchSource.OneTimeLoadCount, 
            new LiveSearchSource(Keyword,
            SelectedStatus.Value, 
            SelectedSort.Value, 
            SelectedProvider.Value,
            _reservation, 
            SearchProvider, 
            NiconicoSession, 
            _nicoLiveCacheRepository)
            );
	}
    

    [RelayCommand]
    public void SearchOptionsUpdated()
    {
        ResetList();
    }
}



public class LiveSearchSource : IIncrementalSource<LiveInfoListItemViewModel>
{
    public LiveSearchSource(
        string keyword,
        SearchLiveStatus status,
        Sort sort,
        LiveProvider provider,
        TimeshiftReservationsResponse? reservationRes,
        SearchProvider searchProvider,
        NiconicoSession niconicoSession,
        NicoLiveCacheRepository nicoLiveCacheRepository
        )
    {
        Keyword = keyword;
        Status = status;
        Sort = sort;
        Provider = provider;
        _reservationRes = reservationRes;
        SearchProvider = searchProvider;
        NiconicoSession = niconicoSession;
        _nicoLiveCacheRepository = nicoLiveCacheRepository;
    }

    private HashSet<string> SearchedVideoIdsHash = new HashSet<string>();
    private TimeshiftReservationsResponse? _reservationRes;

    private readonly NicoLiveCacheRepository _nicoLiveCacheRepository;

    public string Keyword { get; }
    public LiveStatus Status { get; }
    public LiveSort Sort { get; }
    public LiveProvider Provider { get; }
    public SearchProvider SearchProvider { get; }
    public NiconicoSession NiconicoSession { get; }

    public const int OneTimeLoadCount = 40;


    async Task<IEnumerable<LiveInfoListItemViewModel>> IIncrementalSource<LiveInfoListItemViewModel>.GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken ct)
    {
        var res = await NiconicoSession.ToolkitContext.Search.Live.LiveSearchAsync(Keyword, pageIndex + 1, Status, Sort, Provider, ct);

        ct.ThrowIfCancellationRequested();

        if (res.Meta.Status != (int)HttpStatusCode.OK)
        {
            return Enumerable.Empty<LiveInfoListItemViewModel>();
        }

        List<LiveInfoListItemViewModel> items = new();
        foreach (var item in res.Items)
        {
            if (!SearchedVideoIdsHash.Contains(item.ProgramId))
            {
                SearchedVideoIdsHash.Add(item.ProgramId);
                _nicoLiveCacheRepository.AddOrUpdate(new NicoLive()
                {
                    LiveId = item.ProgramId,
                    BroadcasterId = item.ProgramProvider?.Name ?? item.SocialGroup?.Name ?? string.Empty,
                    Title = item.Program.Title,
                });

                var liveInfoVM = new LiveInfoListItemViewModel(item.ProgramId);
                liveInfoVM.Setup(item);

                if (_reservationRes?.Reservations?.Items is { } reservations
                    && reservations.FirstOrDefault(reservation => item.ProgramId == reservation.ProgramId) is { }  reservation
                    )
                {
                    liveInfoVM.SetReservation(reservation);
                }
                else
                {
                    liveInfoVM.SetReservation(null);
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
