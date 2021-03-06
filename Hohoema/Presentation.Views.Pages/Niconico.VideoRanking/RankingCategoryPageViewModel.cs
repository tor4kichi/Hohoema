﻿using Hohoema.Models.Domain.Application;
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Domain.Niconico.Video.Ranking;
using Hohoema.Models.Domain.PageNavigation;
using Hohoema.Models.Domain.Pins;
using Hohoema.Models.Helpers;
using Hohoema.Models.UseCase;
using Hohoema.Models.UseCase.Playlist;
using Hohoema.Presentation.Services;
using Hohoema.Models.UseCase.PageNavigation;
using Hohoema.Presentation.ViewModels.Niconico.Video.Commands;
using Hohoema.Presentation.ViewModels.VideoListPage;
using I18NPortable;
using NiconicoToolkit.Ranking.Video;
using Prism.Navigation;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Uno.Threading;
using Hohoema.Models.Domain.Notification;
using Hohoema.Models.Domain.Niconico;
using NiconicoToolkit.Video;
using NiconicoToolkit.Rss.Video;
using Microsoft.Toolkit.Collections;
using Microsoft.Extensions.Caching.Memory;

namespace Hohoema.Presentation.ViewModels.Pages.Niconico.VideoRanking
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

    public class RankingCategoryPageViewModel 
        : HohoemaListingPageViewModelBase<RankedVideoListItemControlViewModel>,
        INavigatedAwareAsync,
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
        bool _IsNavigateCompleted = false;
        bool _isRequireUpdate;
        bool _nowInitializeRankingTerm = false;

        private RankingGenre _RankingGenre;
        public RankingGenre RankingGenre
        {
            get => _RankingGenre;
            set => SetProperty(ref _RankingGenre, value);
        }

        public ReactivePropertySlim<RankingGenreTag> SelectedRankingTag { get; private set; }
        public ReactiveProperty<RankingTerm?> SelectedRankingTerm { get; private set; }

        public IReadOnlyReactiveProperty<RankingTerm[]> CurrentSelectableRankingTerms { get; }

        public ObservableCollection<RankingGenreTag> PickedTags { get; } = new ObservableCollection<RankingGenreTag>();


        public ReactivePropertySlim<bool> IsFailedRefreshRanking { get; private set; }
        public ReactivePropertySlim<bool> CanChangeRankingParameter { get; private set; }
        public ApplicationLayoutManager ApplicationLayoutManager { get; }
        public PageManager PageManager { get; }
        public NicoVideoProvider NicoVideoProvider { get; }
        public VideoRankingSettings RankingSettings { get; }
        public VideoPlayWithQueueCommand VideoPlayWithQueueCommand { get; }
        public SelectionModeToggleCommand SelectionModeToggleCommand { get; }
        public RankingProvider RankingProvider { get; }

        private readonly NiconicoSession _niconicoSession;
        private readonly NotificationService _notificationService;
        private readonly FastAsyncLock _updateLock = new FastAsyncLock();

        MemoryCache _rankingMemoryCache;
        public RankingCategoryPageViewModel(
            ApplicationLayoutManager applicationLayoutManager,
            NiconicoSession niconicoSession,
            PageManager pageManager,
            NicoVideoProvider nicoVideoProvider,
            RankingProvider rankingProvider,
            VideoRankingSettings rankingSettings,
            NotificationService notificationService,
            VideoPlayWithQueueCommand videoPlayWithQueueCommand,
            SelectionModeToggleCommand selectionModeToggleCommand
            )
        {
            ApplicationLayoutManager = applicationLayoutManager;
            _niconicoSession = niconicoSession;
            PageManager = pageManager;
            NicoVideoProvider = nicoVideoProvider;
            RankingProvider = rankingProvider;
            RankingSettings = rankingSettings;
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
                .Throttle(TimeSpan.FromMilliseconds(250))
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


        public override async Task OnNavigatedToAsync(INavigationParameters parameters)
        {
            using (await _updateLock.LockAsync(NavigationCancellationToken))
            {
                _IsNavigateCompleted = false;

                var mode = parameters.GetNavigationMode();
                
                SelectedRankingTag.Value = null;
                
                if (parameters.TryGetRankingGenre(out var rankingGenre))
                {
                    RankingGenre = rankingGenre;
                }
                else
                {
                    throw new Models.Infrastructure.HohoemaExpception("ランキングページの表示に失敗");
                }

                _isRequireUpdate = RankingGenre != _previousRankingGenre;

                // TODO: 人気のタグ、いつ再更新を掛ける
                try
                {
                    PickedTags.Clear();
                    var tags = await RankingProvider.GetRankingGenreTagsAsync(RankingGenre);
                    foreach (var tag in tags)
                    {
                        PickedTags.Add(tag);
                    }
                }
                catch { }

                if (parameters.TryGetRankingGenreTag(out var queryTag))
                {
                    var tag = PickedTags.FirstOrDefault(x => x.Tag == queryTag);
                    if (tag != null)
                    {
                        SelectedRankingTag.Value = tag;
                    }
                    else
                    {
                        Debug.WriteLine("無効なタグです: " + queryTag);
                        SelectedRankingTag.Value = PickedTags.FirstOrDefault();
                    }
                }

                if (SelectedRankingTag.Value == null)
                {
                    SelectedRankingTag.Value = PickedTags.FirstOrDefault();
                }


                _IsNavigateCompleted = true;

                HasError
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
                        if (selectedTag.Tag != null)
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
                    .AddTo(_NavigatingCompositeDisposable);

                await base.OnNavigatedToAsync(parameters);
            }
        }

        protected override bool CheckNeedUpdateOnNavigateTo(NavigationMode mode)
        {
            if (_isRequireUpdate) { return true; }
            return base.CheckNeedUpdateOnNavigateTo(mode);
        }

        public override void OnNavigatedFrom(INavigationParameters parameters)
        {
            _IsNavigateCompleted = false;
            _previousRankingGenre = RankingGenre;

            base.OnNavigatedFrom(parameters);
        }


        protected override (int, IIncrementalSource<RankedVideoListItemControlViewModel>) GenerateIncrementalSource()
        {
            IsFailedRefreshRanking.Value = false;

            var categoryInfo = RankingGenre;

            IIncrementalSource<RankedVideoListItemControlViewModel> source = null;
            try
            {
                source = new CategoryRankingLoadingSource(RankingGenre, SelectedRankingTag.Value?.Tag, SelectedRankingTerm.Value ?? RankingTerm.Hour, _niconicoSession, NicoVideoProvider, _rankingMemoryCache);

                CanChangeRankingParameter.Value = true;

                return (CategoryRankingLoadingSource.OneTimeLoadCount, source);
            }
            catch
            {
                IsFailedRefreshRanking.Value = true;

                return default;
            }            
        }

        protected override void PostResetList()
        {
            _IsNavigateCompleted = true;

            base.PostResetList();
        }
    }


    public class CategoryRankingLoadingSource : IIncrementalSource<RankedVideoListItemControlViewModel>
    {
        private readonly TimeSpan RankingResponseExpireDuration = TimeSpan.FromMinutes(10);

        private readonly NiconicoSession _niconicoSession;
        private readonly NicoVideoProvider _nicoVideoProvider;
        private readonly MemoryCache _memoryCache;
        RankingOptions _options;
        RssVideoResponse _rankingRssResponse;

        public CategoryRankingLoadingSource(
            RankingGenre genre,
            string tag,
            RankingTerm term,
            NiconicoSession niconicoSession,
            NicoVideoProvider nicoVideoProvider,
            MemoryCache memoryCache
            )
            : base()
        {
            _niconicoSession = niconicoSession;
            _nicoVideoProvider = nicoVideoProvider;
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
            var owners = await _nicoVideoProvider.ResolveVideoOwnersAsync(targetItems.Select(x => x.GetVideoId()));

            ct.ThrowIfCancellationRequested();

            return targetItems.Select((item, offset) =>
            {
                var videoId = item.GetVideoId();
                var itemData = item.GetMoreData();
                var vm = new RankedVideoListItemControlViewModel((uint)(head + offset + 1), videoId, item.GetRankTrimmingTitle(), itemData.ThumbnailUrl, itemData.Length, itemData.PostedAt);

                vm.CommentCount = itemData.CommentCount;
                vm.ViewCount = itemData.WatchCount;
                vm.MylistCount = itemData.MylistCount;

                var owner = owners[videoId];
                vm.ProviderId = owner.OwnerId;
                vm.ProviderName = owner.ScreenName;
                vm.ProviderType = owner.UserType;

                return vm;
            });
        }
    }


    public record RankingOptions(RankingGenre Genre, RankingTerm Term, string Tag);



    public class RankedVideoListItemControlViewModel : VideoListItemControlViewModel
    {
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
