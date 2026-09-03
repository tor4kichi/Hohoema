#nullable enable
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Hohoema.Models.Niconico;
using Hohoema.Models.Niconico.Video;
using Hohoema.Models.Notification;
using Hohoema.Models.PageNavigation;
using Hohoema.Models.Pins;
using Hohoema.Services;
using Hohoema.ViewModels.Niconico.Video.Commands;
using Hohoema.ViewModels.VideoListPage;
using I18NPortable;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.Collections;
using NiconicoToolkit.Ranking.Video;
using NiconicoToolkit.Rss.Video;
using NiconicoToolkit.Video;
using NiconicoToolkit.Video.Watch;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Xaml.Navigation;
using AsyncLock = Hohoema.Helpers.AsyncLock;
using R3;
using Reactive.Bindings.R3.Extensions;

namespace Hohoema.ViewModels.Pages.Niconico.VideoRanking;

public static class RankingCategoryPageNavigationConstants
{
    public const string RankingGenreQueryKey = "genre";
    public const string RankingGenreTagQueryKey = "tag";

    public static INavigationParameters SetRankingGenre(this INavigationParameters parameters, string genreId)
    {
        parameters.Add(RankingGenreQueryKey, Uri.EscapeDataString(genreId));
        return parameters;
    }

    public static bool TryGetRankingGenre(this INavigationParameters parameters, out string outGenreId)
    {
        if (parameters.TryGetValue(RankingGenreQueryKey, out string strGenre))
        {
            outGenreId = strGenre;
            return true;
        }

        outGenreId = RankingGenreConstants.All.Id;
        return false;
    }

    public static INavigationParameters SetRankingGenreTag(this INavigationParameters parameters, string tag)
    {
        if (tag is not null)
        {
            parameters.Add(RankingGenreTagQueryKey, Uri.EscapeDataString(tag));
        }

        return parameters;
    }

    public static bool TryGetRankingGenreTag(this INavigationParameters parameters, out string outTag)
    {
        if (parameters.TryGetValue(RankingGenreTagQueryKey, out string queryTag)
            && !string.IsNullOrEmpty(queryTag)
            )
        {
            outTag = Uri.UnescapeDataString(queryTag);
            return true;
        }
        else
        {
            outTag = null;
            return false;
        }
    }
}

public sealed partial class RankingCategoryPageViewModel 
    : VideoListingPageViewModelBase<RankedVideoListItemControlViewModel>,        
    IPinablePage,
    ITitleUpdatablePage
{
    HohoemaPin IPinablePage.GetPin()
    {
        var genreName = Title;
        var tag = SelectedRankingTag.Value;
        var pickedTag = TrendTags.FirstOrDefault(x => x == tag);

        Dictionary<string, string> pairs = new Dictionary<string, string>();
        pairs.Add(RankingCategoryPageNavigationConstants.RankingGenreQueryKey, RankingGenreId.ToString());
        if (!string.IsNullOrEmpty(pickedTag) && pickedTag != "all")
        {
            pairs.Add(RankingCategoryPageNavigationConstants.RankingGenreTagQueryKey, pickedTag);
        }
        
        return new HohoemaPin()
        {
            Label = pickedTag != null ? $"{pickedTag} - {genreName}" : $"{genreName}",
            PageType = HohoemaPageType.RankingCategory,
            Parameter = pairs.ToQueryString()
        };
    }

    IObservable<string> ITitleUpdatablePage.GetTitleObservable()
    {
        return this.ObservePropertyChanged(x => x.RankingGenreId)
            .Select(genre => RankingGenreConstants.AllGenres.FirstOrDefault(x => x.Id == genre)?.Name ?? "")
            .AsSystemObservable();
    }


    private static string? _previousRankingGenreId;
    private static string? _prevRankingGenreTag;
    bool _IsNavigateCompleted = false;
    bool _isRequireUpdate;
    bool _nowInitializeRankingTerm = false;

    private string _RankingGenreId;
    public string RankingGenreId
    {
        get => _RankingGenreId;
        set => SetProperty(ref _RankingGenreId, value);
    }

    public BindableReactiveProperty<string> SelectedRankingTag { get; }
    public BindableReactiveProperty<RankingTerm?> SelectedRankingTerm { get; }

    public BindableReactiveProperty<RankingTerm[]> CurrentSelectableRankingTerms { get; }




    public ObservableCollection<string> TrendTags { get; } = [];


    public BindableReactiveProperty<bool> IsFailedRefreshRanking { get; }
    public BindableReactiveProperty<bool> CanChangeRankingParameter { get; }
    public ApplicationLayoutManager ApplicationLayoutManager { get; }
    public NicoVideoProvider NicoVideoProvider { get; }
    public VideoRankingSettings RankingSettings { get; }
    public VideoPlayWithQueueCommand VideoPlayWithQueueCommand { get; }
    public SelectionModeToggleCommand SelectionModeToggleCommand { get; }
    
    private readonly NiconicoSession _niconicoSession;
    private readonly VideoFilteringSettings _videoFilteringSettings;
    private readonly NotificationService _notificationService;
    private readonly AsyncLock _updateLock = new AsyncLock();

    MemoryCache _rankingMemoryCache;
    public RankingCategoryPageViewModel(
        IMessenger messenger,
        ILoggerFactory loggerFactory,
        ApplicationLayoutManager applicationLayoutManager,
        NiconicoSession niconicoSession,
        NicoVideoProvider nicoVideoProvider,        
        VideoRankingSettings rankingSettings,
        VideoFilteringSettings videoFilteringSettings,
        NotificationService notificationService,
        VideoPlayWithQueueCommand videoPlayWithQueueCommand,
        SelectionModeToggleCommand selectionModeToggleCommand
        )
        : base(messenger, loggerFactory.CreateLogger<RankingCategoryPageViewModel>(), disposeItemVM: false)
    {
        ApplicationLayoutManager = applicationLayoutManager;
        _niconicoSession = niconicoSession;
        NicoVideoProvider = nicoVideoProvider;
        RankingSettings = rankingSettings;
        _videoFilteringSettings = videoFilteringSettings;
        _notificationService = notificationService;
        VideoPlayWithQueueCommand = videoPlayWithQueueCommand;
        SelectionModeToggleCommand = selectionModeToggleCommand;

        _rankingMemoryCache = new MemoryCache(new MemoryCacheOptions())
            .AddTo(_CompositeDisposable);

        IsFailedRefreshRanking = new BindableReactiveProperty<bool>(false)
            .AddTo(_CompositeDisposable);
        CanChangeRankingParameter = new BindableReactiveProperty<bool>(false)
            .AddTo(_CompositeDisposable);

        SelectedRankingTag = new BindableReactiveProperty<string>()
            .AddTo(_CompositeDisposable);
        SelectedRankingTerm = new BindableReactiveProperty<RankingTerm?>(RankingTerm.Hour)
            .AddTo(_CompositeDisposable);

        CurrentSelectableRankingTerms = new[]
        {
            this.ObservePropertyChanged(x => RankingGenreId).AsUnitObservable(),
            SelectedRankingTag.AsUnitObservable()
        }
        .Merge()
        .ObserveOnCurrentSynchronizationContext()
        .Select(x =>
        {
            if (SelectedRankingTag.Value == "all")
            {
                return VideoRankingConstants.AllRankingTerms;
            }
            else
            {
                return VideoRankingConstants.GenreWithTagAccepteRankingTerms;
            }
        })
        .ToBindableReactiveProperty()
            .AddTo(_CompositeDisposable);
    }

    private (string? genreId, string? tag) GetRankingParameters(INavigationParameters parameters)
    {
        return (parameters.TryGetRankingGenre(out var rankingGenre) ? rankingGenre : RankingGenreConstants.All.Id, parameters.TryGetRankingGenreTag(out var queryTag) ? queryTag : null);
    }

    string? _navigationParamRankingTag;
    CancellationTokenSource? _navigationCts;
    
    public override async Task OnNavigatedToAsync(INavigationParameters parameters)
    {
        _navigationCts = new CancellationTokenSource();
        var ct = _navigationCts.Token;
        using (await _updateLock.LockAsync())
        {
            _IsNavigateCompleted = false;

            var mode = parameters.GetNavigationMode();

            SelectedRankingTag.Value = null;

            var (rankingGenre, rankingGenreTag) = GetRankingParameters(parameters);

            if (rankingGenre == null)
            {
                throw new Infra.HohoemaException("ランキングページの表示に失敗");
            }

            if (rankingGenre != RankingGenreId)
            {
                TrendTags.Clear();
            }
            RankingGenreId = rankingGenre;
            _navigationParamRankingTag = rankingGenreTag;
            _isRequireUpdate = RankingGenreId != _previousRankingGenreId;           

            if (rankingGenreTag is not null)
            {
                SelectedRankingTag.Value = rankingGenreTag;
            }

            if (SelectedRankingTag.Value == null)
            {
                SelectedRankingTag.Value = TrendTags.FirstOrDefault();
            }

            _IsNavigateCompleted = true;
        }

        DisposableBuilder db = new();

        new[] {
            this.ObservePropertyChanged(x => RankingGenreId, false).AsUnitObservable(),
            SelectedRankingTag.DistinctUntilChanged().AsUnitObservable().Skip(1),
            SelectedRankingTerm.DistinctUntilChanged().Skip(1).Where(x => !_nowInitializeRankingTerm).AsUnitObservable()
        }
            .Merge()
            .Where(_ => _IsNavigateCompleted && !NowLoading)
            .ThrottleLast(TimeSpan.FromMilliseconds(50))
            .ObserveOnCurrentSynchronizationContext()            
            .Subscribe(__ =>
            {
                ResetList();
            })
            .AddTo(ref db);

        CurrentSelectableRankingTerms
            .Delay(TimeSpan.FromMilliseconds(50))
            .ObserveOnCurrentSynchronizationContext()
            .Subscribe(x =>
            {
                if (x == null) { return; }
                _nowInitializeRankingTerm = true;
                SelectedRankingTerm.Value = x[0];
                _nowInitializeRankingTerm = false;
            })
            .AddTo(ref db);

        TrendTags.CollectionChangedAsObservable()
            .ThrottleLast(TimeSpan.FromMilliseconds(100))
            .ObserveOnCurrentSynchronizationContext()
            .Subscribe(tags =>
            {
                if (_navigationParamRankingTag == SelectedRankingTag.Value)
                {
                    SelectedRankingTag.ForceNotify();
                }
            })
            .AddTo(ref db);

        db.Build().RegisterTo(ct);

        await base.OnNavigatedToAsync(parameters);            
    }

    protected override bool CheckNeedUpdateOnNavigateTo(NavigationMode mode, INavigationParameters parameters)
    {
        var (rankingGenre, rankingGenreTag) = GetRankingParameters(parameters);
        if (rankingGenre == RankingGenreId && rankingGenreTag == SelectedRankingTag.Value)
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    public override void OnNavigatedFrom(INavigationParameters parameters)
    {
        _navigationCts?.Cancel();
        _navigationCts?.Dispose();
        _navigationCts = null;
        _IsNavigateCompleted = false;
        _previousRankingGenreId = RankingGenreId;
        _prevRankingGenreTag = SelectedRankingTag.Value;

        base.OnNavigatedFrom(parameters);
    }


    protected override (int, IIncrementalSource<RankedVideoListItemControlViewModel>) GenerateIncrementalSource()
    {
        IsFailedRefreshRanking.Value = false;
        try
        {
            var source = new Nvapi_CategoryRankingLoadingSource(RankingGenreId, SelectedRankingTag.Value, SelectedRankingTerm.Value ?? RankingTerm.Hour, _niconicoSession, _rankingMemoryCache, TrendTags);
            CanChangeRankingParameter.Value = true;
            return (Nvapi_CategoryRankingLoadingSource.OneTimeLoadCount, source);
        }
        catch
        {
            IsFailedRefreshRanking.Value = true;
            throw;
        }            
    }

    protected override void PostResetList()
    {
        _IsNavigateCompleted = true;

        base.PostResetList();
    }
}


public class Nvapi_CategoryRankingLoadingSource : IIncrementalSource<RankedVideoListItemControlViewModel>
{
    private readonly TimeSpan RankingResponseExpireDuration = TimeSpan.FromMinutes(10);

    private readonly NiconicoSession _niconicoSession;
    private readonly MemoryCache _memoryCache;
    RankingOptions _options;

    public ObservableCollection<string> TrendTags { get; }

    public Nvapi_CategoryRankingLoadingSource(
        string genreId,
        string tag,
        RankingTerm term,
        NiconicoSession niconicoSession,
        MemoryCache memoryCache,
        ObservableCollection<string> trendTags)
        : base()
    {
        _niconicoSession = niconicoSession;
        _memoryCache = memoryCache;
        _options = new RankingOptions(genreId, term, tag);
        TrendTags = trendTags;
    }

    bool _hasNext = true;
    public const int OneTimeLoadCount = 25;
    private int MaxItemsCount = 100;
    private async ValueTask<IEnumerable<NvapiVideoItem>> GetCachedRankingAsync(int page, CancellationToken ct)
    {
        string key = $"{_options}_{page}";
        if (_memoryCache.TryGetValue<List<NvapiVideoItem>>(key, out var items))
        {
            Debug.WriteLine($"RankingItems get from cache: {key}");
            return items;
        }
        else 
        {
            Debug.WriteLine($"RankingItems get from online: {key}");
            
            var res = await _niconicoSession.ToolkitContext.Video.Ranking.GetRankingAsync(_options.GenreId, _options.Term, _options.Tag, page, ct);
            if (res.IsSuccess)
            {
                if (TrendTags.Count == 0)
                {
                    TrendTags.Add("all");
                    var tags = res.Data.Response.GetTeibanRankingFeaturedKeyAndTrendTags.Data.TrendTags;
                    foreach (var tag in tags ?? [])
                    {
                        TrendTags.Add(tag);
                    }
                }
                _memoryCache.Set(key, res.Data.Response.GetTeibanRanking.Data.Items, TimeSpan.FromMinutes(5));
                return res.Data.Response.GetTeibanRanking.Data.Items;
            }
            else
            {
                Debug.WriteLine($"RankingItems get from online (no more items): {key}");
                return new List<NvapiVideoItem>();
            }
        }
    }

    int _itemsCount = 0;
    async Task<IEnumerable<RankedVideoListItemControlViewModel>> IIncrementalSource<RankedVideoListItemControlViewModel>.GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken ct)
    { 
        if (!_hasNext) { return []; }       
        try
        {
            var targetItems = await GetCachedRankingAsync(pageIndex + 1, ct);

            _hasNext = targetItems.Count() >= 99;
            ct.ThrowIfCancellationRequested();
            int startCount = _itemsCount;
            _itemsCount += targetItems.Count();
            return targetItems.Select((item, offset) =>
            {
                return new RankedVideoListItemControlViewModel((uint)(startCount + offset) + 1, item);
            }).ToArray();
        }
        catch
        {
            _hasNext = false;
            throw;
        }
    }
}


public record RankingOptions(string GenreId, RankingTerm Term, string Tag);



public class RankedVideoListItemControlViewModel : VideoListItemControlViewModel
{
    public RankedVideoListItemControlViewModel(
        uint rank, NvapiVideoItem nvapiVideoItem
        )
        : base(nvapiVideoItem)
    {
        Rank = rank;
    }

    public RankedVideoListItemControlViewModel(
        uint rank, NicoVideo data
        )
        : base(data)
    {
        Rank = rank;
    }

    public RankedVideoListItemControlViewModel(uint rank, string rawVideoId, string title, string thumbnailUrl, TimeSpan videoLength, DateTime postedAt) 
        : base(rawVideoId, title, thumbnailUrl, videoLength, postedAt)
    {
        Rank = rank;
    }

    public uint Rank { get; }
}
