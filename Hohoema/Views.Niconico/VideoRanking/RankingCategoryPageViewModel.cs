using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hohoema.Contracts.Services.Navigations;
using Hohoema.Models.Niconico;
using Hohoema.Models.Niconico.Video;
using Hohoema.Models.Niconico.Video.Ranking;
using Hohoema.Models.Notification;
using Hohoema.Models.PageNavigation;
using Hohoema.Models.Pins;
using Hohoema.Services;
using Hohoema.Services.Navigations;
using Hohoema.ViewModels.Niconico.Video.Commands;
using Hohoema.ViewModels.VideoListPage;
using I18NPortable;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.Collections;
using NiconicoToolkit.Ranking.Video;
using NiconicoToolkit.Rss.Video;
using NiconicoToolkit.Video;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Xaml.Navigation;
using AsyncLock = Hohoema.Helpers.AsyncLock;

namespace Hohoema.ViewModels.Pages.Niconico.VideoRanking
{
    public static class RankingCategoryPageNavigationConstants
    {
        public const string RankingGenreQueryKey = "genre";
        public const string RankingGenreTagQueryKey = "tag";

        public static INavigationParameters SetRankingGenre(this INavigationParameters parameters, RankingGenre rankingGenre)
        {
            parameters.Add(RankingGenreQueryKey, Uri.EscapeDataString(rankingGenre.ToString()));
            return parameters;
        }

        public static bool TryGetRankingGenre(this INavigationParameters parameters, out RankingGenre outGenre)
        {
            if (parameters.TryGetValue(RankingGenreQueryKey, out string strGenre))
            {
                if (Enum.TryParse(strGenre, out RankingGenre enumGenre))
                {
                    outGenre = enumGenre;
                    return true;
                }
            }

            outGenre = RankingGenre.All;
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
        : HohoemaListingPageViewModelBase<RankedVideoListItemControlViewModel>,        
        IPinablePage,
        ITitleUpdatablePage
    {
        HohoemaPin IPinablePage.GetPin()
        {
            var genreName = RankingGenre.Translate();
            var tag = SelectedRankingTag.Value?.Tag;
            var pickedTag = PickedTags.FirstOrDefault(x => x.Tag == tag);

            Dictionary<string, string> pairs = new Dictionary<string, string>();
            pairs.Add(RankingCategoryPageNavigationConstants.RankingGenreQueryKey, RankingGenre.ToString());
            if (!string.IsNullOrEmpty(pickedTag.Tag) && pickedTag.Tag != "all")
            {
                pairs.Add(RankingCategoryPageNavigationConstants.RankingGenreTagQueryKey, pickedTag.Tag);
            }
            
            return new HohoemaPin()
            {
                Label = pickedTag != null ? $"{pickedTag.Label} - {genreName}" : $"{genreName}",
                PageType = HohoemaPageType.RankingCategory,
                Parameter = pairs.ToQueryString()
            };
        }

        IObservable<string> ITitleUpdatablePage.GetTitleObservable()
        {
            return this.ObserveProperty(x => x.RankingGenre)
                .Select(genre => "RankingTitleWithGenre".Translate(genre.Translate()));
        }


        private static RankingGenre? _previousRankingGenre;
        private static RankingGenreTag _prevRankingGenreTag;
        bool _IsNavigateCompleted = false;
        bool _isRequireUpdate;
        bool _nowInitializeRankingTerm = false;

        private RankingGenre _RankingGenre;
        public RankingGenre RankingGenre
        {
            get => _RankingGenre;
            set => SetProperty(ref _RankingGenre, value);
        }

        public ReactivePropertySlim<RankingGenreTag> SelectedRankingTag { get; }
        public ReactiveProperty<RankingTerm?> SelectedRankingTerm { get; }

        public IReadOnlyReactiveProperty<RankingTerm[]> CurrentSelectableRankingTerms { get; }


        

        public ObservableCollection<RankingGenreTag> PickedTags { get; } = new ObservableCollection<RankingGenreTag>();


        public ReactivePropertySlim<bool> IsFailedRefreshRanking { get; }
        public ReactivePropertySlim<bool> CanChangeRankingParameter { get; }
        public ApplicationLayoutManager ApplicationLayoutManager { get; }
        public PageManager PageManager { get; }
        public NicoVideoProvider NicoVideoProvider { get; }
        public VideoRankingSettings RankingSettings { get; }
        public VideoPlayWithQueueCommand VideoPlayWithQueueCommand { get; }
        public SelectionModeToggleCommand SelectionModeToggleCommand { get; }
        public RankingProvider RankingProvider { get; }

        private readonly NiconicoSession _niconicoSession;
        private readonly VideoFilteringSettings _videoFilteringSettings;
        private readonly NotificationService _notificationService;
        private readonly AsyncLock _updateLock = new AsyncLock();

        MemoryCache _rankingMemoryCache;
        public RankingCategoryPageViewModel(
            ILoggerFactory loggerFactory,
            ApplicationLayoutManager applicationLayoutManager,
            NiconicoSession niconicoSession,
            PageManager pageManager,
            NicoVideoProvider nicoVideoProvider,
            RankingProvider rankingProvider,
            VideoRankingSettings rankingSettings,
            VideoFilteringSettings videoFilteringSettings,
            NotificationService notificationService,
            VideoPlayWithQueueCommand videoPlayWithQueueCommand,
            SelectionModeToggleCommand selectionModeToggleCommand
            )
            : base(loggerFactory.CreateLogger<RankingCategoryPageViewModel>())
        {
            ApplicationLayoutManager = applicationLayoutManager;
            _niconicoSession = niconicoSession;
            PageManager = pageManager;
            NicoVideoProvider = nicoVideoProvider;
            RankingProvider = rankingProvider;
            RankingSettings = rankingSettings;
            _videoFilteringSettings = videoFilteringSettings;
            _notificationService = notificationService;
            VideoPlayWithQueueCommand = videoPlayWithQueueCommand;
            SelectionModeToggleCommand = selectionModeToggleCommand;

            _rankingMemoryCache = new MemoryCache(new MemoryCacheOptions())
                .AddTo(_CompositeDisposable);

            IsFailedRefreshRanking = new ReactivePropertySlim<bool>(false)
                .AddTo(_CompositeDisposable);
            CanChangeRankingParameter = new ReactivePropertySlim<bool>(false)
                .AddTo(_CompositeDisposable);

            SelectedRankingTag = new ReactivePropertySlim<RankingGenreTag>()
                .AddTo(_CompositeDisposable);
            SelectedRankingTerm = new ReactiveProperty<RankingTerm?>(RankingTerm.Hour)
                .AddTo(_CompositeDisposable);

            CurrentSelectableRankingTerms = new[]
            {
                this.ObserveProperty(x => RankingGenre).ToUnit(),
                SelectedRankingTag.ToUnit()
            }
            .CombineLatest()
            .Select(x =>
            {
                if (RankingGenre != RankingGenre.HotTopic)
                {
                    if (string.IsNullOrEmpty(SelectedRankingTag.Value?.Tag))
                    {
                        return VideoRankingConstants.AllRankingTerms;
                    }
                    else
                    {
                        return VideoRankingConstants.GenreWithTagAccepteRankingTerms;
                    }
                }
                else
                {
                    return VideoRankingConstants.HotTopicAccepteRankingTerms;
                }
            })
            .ToReadOnlyReactivePropertySlim()
                .AddTo(_CompositeDisposable);

            new[] {
                this.ObserveProperty(x => RankingGenre).ToUnit(),
                SelectedRankingTag.ToUnit(),
                SelectedRankingTerm.Where(x => !_nowInitializeRankingTerm).ToUnit()
            }
                .CombineLatest()
                .Where(_ => _IsNavigateCompleted)
                .Throttle(TimeSpan.FromMilliseconds(50))
                .Subscribe(__ =>
                {
                    ResetList();
                })
                .AddTo(_CompositeDisposable);

            CurrentSelectableRankingTerms
                .Delay(TimeSpan.FromMilliseconds(50))
                .Subscribe(x =>
                {
                    _nowInitializeRankingTerm = true;
                    SelectedRankingTerm.Value = x[0];
                    _nowInitializeRankingTerm = false;
                })
                .AddTo(_CompositeDisposable);
        }

        private (RankingGenre? genre, string? tag) GetRankingParameters(INavigationParameters parameters)
        {
            return (parameters.TryGetRankingGenre(out var rankingGenre) ? rankingGenre : null, parameters.TryGetRankingGenreTag(out var queryTag) ? queryTag : null);
        }

        public override async Task OnNavigatedToAsync(INavigationParameters parameters)
        {
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

                RankingGenre = rankingGenre.Value;

                _isRequireUpdate = RankingGenre != _previousRankingGenre;

                // TODO: 人気のタグ、いつ再更新を掛ける
                try
                {
                    PickedTags.Clear();
                    PickedTags.Add(new RankingGenreTag() { Genre = RankingGenre });
                    var tags = await RankingProvider.GetRankingGenreTagsAsync(RankingGenre);
                    foreach (var tag in tags)
                    {
                        PickedTags.Add(tag);
                    }
                }
                catch { }

                if (rankingGenreTag is not null)
                {
                    var tag = PickedTags.FirstOrDefault(x => x.Tag == rankingGenreTag);
                    if (tag != null)
                    {
                        SelectedRankingTag.Value = tag;
                    }
                    else
                    {
                        Debug.WriteLine("無効なタグです: " + rankingGenreTag);
                        SelectedRankingTag.Value = PickedTags.FirstOrDefault();
                    }
                }

                if (SelectedRankingTag.Value == null)
                {
                    SelectedRankingTag.Value = PickedTags.FirstOrDefault();
                }

                _IsNavigateCompleted = true;

                this.ObserveProperty(x => x.HasError)
                    .Where(x => x)
                    .Subscribe(async _ =>
                {
                    try
                    {
                        try
                        {
                            var tags = await RankingProvider.GetRankingGenreTagsAsync(RankingGenre, isForceUpdate: true);
                            PickedTags.Clear();
                            foreach (var tag in tags)
                            {
                                PickedTags.Add(tag);
                            }
                        }
                        catch
                        {
                            return;
                        }

                        var sameGenreFavTags = RankingSettings.FavoriteTags.Where(x => x.Genre == RankingGenre).ToArray();
                        foreach (var oldFavTag in sameGenreFavTags)
                        {
                            if (false == PickedTags.Any(x => x.Tag == oldFavTag.Tag))
                            {
                                RankingSettings.RemoveFavoriteTag(RankingGenre, oldFavTag.Tag);
                            }
                        }

                        var selectedTag = SelectedRankingTag.Value;
                        if (selectedTag != null && selectedTag.Tag != null)
                        {
                            if (false == PickedTags.Any(x => x.Tag == selectedTag.Tag))
                            {
                                SelectedRankingTag.Value = PickedTags.ElementAtOrDefault(0);

                                // TODO: i18n：人気タグがオンライン側で外れた場合の通知
                                _notificationService.ShowLiteInAppNotification($"「{selectedTag.Label}」は人気のタグの一覧から外れたようです", DisplayDuration.MoreAttention);
                            }
                        }
                    }
                    catch
                    {

                    }
                })
                    .AddTo(_navigationDisposables);
            }
            
            await base.OnNavigatedToAsync(parameters);            
        }

        protected override bool CheckNeedUpdateOnNavigateTo(NavigationMode mode, INavigationParameters parameters)
        {
            var (rankingGenre, rankingGenreTag) = GetRankingParameters(parameters);
            if (rankingGenre == RankingGenre && rankingGenreTag == SelectedRankingTag.Value?.Tag)
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
            _IsNavigateCompleted = false;
            _previousRankingGenre = RankingGenre;
            _prevRankingGenreTag = SelectedRankingTag.Value;

            base.OnNavigatedFrom(parameters);
        }


        protected override (int, IIncrementalSource<RankedVideoListItemControlViewModel>) GenerateIncrementalSource()
        {
            IsFailedRefreshRanking.Value = false;

            var categoryInfo = RankingGenre;

            IIncrementalSource<RankedVideoListItemControlViewModel> source = null;
            try
            {
                if (IsRssRankingSource is false)
                {
                    source = new Nvapi_CategoryRankingLoadingSource(RankingGenre, SelectedRankingTag.Value?.Tag, SelectedRankingTerm.Value ?? RankingTerm.Hour, _niconicoSession, NicoVideoProvider, _videoFilteringSettings, _rankingMemoryCache);
                    CanChangeRankingParameter.Value = true;
                    return (Nvapi_CategoryRankingLoadingSource.OneTimeLoadCount, source);
                }
                else
                {                    
                    source = new Rss_CategoryRankingLoadingSource(RankingGenre, SelectedRankingTag.Value?.Tag, SelectedRankingTerm.Value ?? RankingTerm.Hour, _niconicoSession, NicoVideoProvider, _videoFilteringSettings, _rankingMemoryCache);
                    CanChangeRankingParameter.Value = true;
                    return (Rss_CategoryRankingLoadingSource.OneTimeLoadCount, source);
                }
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

        [ObservableProperty]
        private bool _isRssRankingSource;

        [RelayCommand]
        private void ToggleRankingItemsSource()
        {
            IsRssRankingSource = !IsRssRankingSource;
            ResetList();
        }
    }


    public class Rss_CategoryRankingLoadingSource : IIncrementalSource<RankedVideoListItemControlViewModel>
    {
        private readonly TimeSpan RankingResponseExpireDuration = TimeSpan.FromMinutes(10);

        private readonly NiconicoSession _niconicoSession;
        private readonly NicoVideoProvider _nicoVideoProvider;
        private readonly VideoFilteringSettings _videoFilteringSettings;
        private readonly MemoryCache _memoryCache;
        RankingOptions _options;
        RssVideoResponse _rankingRssResponse;

        public Rss_CategoryRankingLoadingSource(
            RankingGenre genre,
            string tag,
            RankingTerm term,
            NiconicoSession niconicoSession,
            NicoVideoProvider nicoVideoProvider,
            VideoFilteringSettings videoFilteringSettings,
            MemoryCache memoryCache
            )
            : base()
        {
            _niconicoSession = niconicoSession;
            _nicoVideoProvider = nicoVideoProvider;
            _videoFilteringSettings = videoFilteringSettings;
            _memoryCache = memoryCache;
            _options = new RankingOptions(genre, term, tag);
        }

        public const int OneTimeLoadCount = 20;


        private async ValueTask<RssVideoResponse> GetCachedRankingRssAsync()
        {
            if (_memoryCache.TryGetValue<RssVideoResponse>(_options, out var res))
            {
                return res;
            }
            else
            {
                res = await _niconicoSession.ToolkitContext.Video.Ranking.GetRankingRssAsync(_options.Genre, _options.Tag, _options.Term);
                if (res.IsOK)
                {
                    _memoryCache.Set(_options, res, TimeSpan.FromMinutes(5));
                }
                return res;
            }
        }

        async Task<IEnumerable<RankedVideoListItemControlViewModel>> IIncrementalSource<RankedVideoListItemControlViewModel>.GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken ct)
        {
            _rankingRssResponse ??= await GetCachedRankingRssAsync();

            ct.ThrowIfCancellationRequested();

            int head = pageIndex * pageSize;
            var targetItems = _rankingRssResponse.Items.Skip(head).Take(pageSize);

            ct.ThrowIfCancellationRequested();

            return targetItems.Select((item, offset) =>
            {
                var videoId = item.GetVideoId();
                var itemData = item.GetMoreData();
                var vm = new RankedVideoListItemControlViewModel((uint)(head + offset + 1), videoId, item.GetRankTrimmingTitle(), itemData.ThumbnailUrl, itemData.Length, itemData.PostedAt);

                vm.CommentCount = itemData.CommentCount;
                vm.ViewCount = itemData.WatchCount;
                vm.MylistCount = itemData.MylistCount;

                return vm;
            })
            .ToArray()// Note: IncrementalLoadingSourceが複数回呼び出すためFreezeしたい
            ;
        }
    }

    public class Nvapi_CategoryRankingLoadingSource : IIncrementalSource<RankedVideoListItemControlViewModel>
    {
        private readonly TimeSpan RankingResponseExpireDuration = TimeSpan.FromMinutes(10);

        private readonly NiconicoSession _niconicoSession;
        private readonly NicoVideoProvider _nicoVideoProvider;
        private readonly VideoFilteringSettings _videoFilteringSettings;
        private readonly MemoryCache _memoryCache;
        RankingOptions _options;
        VideoRankingResponse _rankingRssResponse;

        public Nvapi_CategoryRankingLoadingSource(
            RankingGenre genre,
            string tag,
            RankingTerm term,
            NiconicoSession niconicoSession,
            NicoVideoProvider nicoVideoProvider,
            VideoFilteringSettings videoFilteringSettings,
            MemoryCache memoryCache
            )
            : base()
        {
            _niconicoSession = niconicoSession;
            _nicoVideoProvider = nicoVideoProvider;
            _videoFilteringSettings = videoFilteringSettings;
            _memoryCache = memoryCache;
            _options = new RankingOptions(genre, term, tag);
        }

        public const int OneTimeLoadCount = 100;

        private async ValueTask<List<NvapiVideoItem>> GetCachedRankingAsync(int page, CancellationToken ct)
        {
            string key = _options.ToString() + page;
            if (_memoryCache.TryGetValue<List<NvapiVideoItem>>(key, out var items))
            {
                Debug.WriteLine($"RankingItems get from cache: {key}");
                return items;
            }
            else 
            {
                Debug.WriteLine($"RankingItems get from online: {key}");
                var res = await _niconicoSession.ToolkitContext.Video.Ranking.GetRankingAsync(_options.Genre, _options.Term, _options.Tag, page, ct);
                if (res.IsSuccess)
                {
                    _memoryCache.Set(key, res.Data.Items, TimeSpan.FromMinutes(5));
                    return res.Data.Items;
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
            var targetItems = await GetCachedRankingAsync(pageIndex + 1, ct);

            ct.ThrowIfCancellationRequested();
            return targetItems.Select((item, offset) =>
            {
                return new RankedVideoListItemControlViewModel((uint)(_itemsCount++) + 1, item);
            }).ToArray();
        }
    }


    public record RankingOptions(RankingGenre Genre, RankingTerm Term, string Tag);



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
}
