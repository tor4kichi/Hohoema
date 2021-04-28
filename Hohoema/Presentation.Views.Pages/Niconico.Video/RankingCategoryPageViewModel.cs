using Hohoema.Models.Domain.Application;
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Domain.PageNavigation;
using Hohoema.Models.Domain.Pins;
using Hohoema.Models.Helpers;
using Hohoema.Models.UseCase;
using Hohoema.Models.UseCase.NicoVideos;
using Hohoema.Presentation.Services;
using Hohoema.Presentation.Services.Page;
using Hohoema.Presentation.ViewModels.Niconico.Video.Commands;
using Hohoema.Presentation.ViewModels.VideoListPage;
using I18NPortable;
using Mntone.Nico2.Videos.Ranking;
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

namespace Hohoema.Presentation.ViewModels.Pages.Niconico.Video
{
    public class RankingCategoryPageViewModel 
        : HohoemaListingPageViewModelBase<RankedVideoInfoControlViewModel>,
        INavigatedAwareAsync,
        IPinablePage,
        ITitleUpdatablePage
    {
        HohoemaPin IPinablePage.GetPin()
        {
            var genreName = RankingGenre.Translate();
            var tag = SelectedRankingTag.Value?.Tag;
            var pickedTag = PickedTags.FirstOrDefault(x => x.Tag == tag);
            string parameter = null;
            if (string.IsNullOrEmpty(pickedTag?.Tag) || pickedTag.Tag == "all")
            {
                pickedTag = null;
                parameter = $"genre={RankingGenre}";
            }
            else
            {
                parameter = $"genre={RankingGenre}&tag={Uri.EscapeDataString(SelectedRankingTag.Value.Tag)}";
            }
            return new HohoemaPin()
            {
                Label = pickedTag != null ? $"{pickedTag.Label} - {genreName}" : $"{genreName}",
                PageType = HohoemaPageType.RankingCategory,
                Parameter = parameter
            };
        }

        IObservable<string> ITitleUpdatablePage.GetTitleObservable()
        {
            return this.ObserveProperty(x => x.RankingGenre)
                .Select(genre => "RankingTitleWithGenre".Translate(genre.Translate()));
        }

        static FastAsyncLock _updateLock = new FastAsyncLock();


        public RankingCategoryPageViewModel(
            ApplicationLayoutManager applicationLayoutManager,
            PageManager pageManager,
            HohoemaPlaylist hohoemaPlaylist,
            NicoVideoProvider nicoVideoProvider,
            RankingProvider rankingProvider,
            VideoRankingSettings rankingSettings,
            IScheduler scheduler,
            NotificationService notificationService,
            SelectionModeToggleCommand selectionModeToggleCommand
            )
        {
            ApplicationLayoutManager = applicationLayoutManager;
            PageManager = pageManager;
            HohoemaPlaylist = hohoemaPlaylist;
            NicoVideoProvider = nicoVideoProvider;
            RankingProvider = rankingProvider;
            RankingSettings = rankingSettings;
            _scheduler = scheduler;
            _notificationService = notificationService;
            SelectionModeToggleCommand = selectionModeToggleCommand;
            IsFailedRefreshRanking = new ReactiveProperty<bool>(false)
                .AddTo(_CompositeDisposable);
            CanChangeRankingParameter = new ReactiveProperty<bool>(false)
                .AddTo(_CompositeDisposable);

            SelectedRankingTag = new ReactiveProperty<RankingGenreTag>();
            SelectedRankingTerm = new ReactiveProperty<RankingTerm?>(RankingTerm.Hour);

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
                        return NiconicoRanking.Constants.AllRankingTerms;
                    }
                    else
                    {
                        return NiconicoRanking.Constants.GenreWithTagAccepteRankingTerms;
                    }
                }
                else
                {
                    return NiconicoRanking.Constants.HotTopicAccepteRankingTerms;
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
                    _ = ResetList();
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

            

        

        bool _nowInitializeRankingTerm = false;

        private RankingGenre _RankingGenre;
        public RankingGenre RankingGenre
        {
            get => _RankingGenre;
            set => SetProperty(ref _RankingGenre, value);
        }

        public ReactiveProperty<RankingGenreTag> SelectedRankingTag { get; private set; }
        public ReactiveProperty<RankingTerm?> SelectedRankingTerm { get; private set; }

        public IReadOnlyReactiveProperty<RankingTerm[]> CurrentSelectableRankingTerms { get; }

        public ObservableCollection<RankingGenreTag> PickedTags { get; } = new ObservableCollection<RankingGenreTag>();


        public ReactiveProperty<bool> IsFailedRefreshRanking { get; private set; }
        public ReactiveProperty<bool> CanChangeRankingParameter { get; private set; }
        public ApplicationLayoutManager ApplicationLayoutManager { get; }
        public PageManager PageManager { get; }
        public HohoemaPlaylist HohoemaPlaylist { get; }
        public NicoVideoProvider NicoVideoProvider { get; }
        public VideoRankingSettings RankingSettings { get; }
        public SelectionModeToggleCommand SelectionModeToggleCommand { get; }
        public RankingProvider RankingProvider { get; }
        
        private static RankingGenre? _previousRankingGenre;
        private readonly IScheduler _scheduler;
        private readonly NotificationService _notificationService;
        bool _IsNavigateCompleted = false;

        public override async Task OnNavigatedToAsync(INavigationParameters parameters)
        {
            using (await _updateLock.LockAsync(NavigationCancellationToken))
            {
                _IsNavigateCompleted = false;

                var mode = parameters.GetNavigationMode();
                if (mode == NavigationMode.New)
                {
                    SelectedRankingTag.Value = null;
                    if (parameters.TryGetValue("genre", out RankingGenre genre))
                    {
                        RankingGenre = genre;
                    }
                    else if (parameters.TryGetValue("genre", out string genreString))
                    {
                        if (Enum.TryParse(genreString, out genre))
                        {
                            RankingGenre = genre;
                        }
                    }
                    else
                    {
                        throw new Exception("ランキングページの表示に失敗");
                    }

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

                    if (parameters.TryGetValue("tag", out string queryTag))
                    {
                        if (!string.IsNullOrEmpty(queryTag))
                        {
                            var unescapedTagString = Uri.UnescapeDataString(queryTag);

                            var tag = PickedTags.FirstOrDefault(x => x.Tag == unescapedTagString);
                            if (tag != null)
                            {
                                SelectedRankingTag.Value = tag;
                            }
                            else
                            {
                                Debug.WriteLine("無効なタグです: " + unescapedTagString);
                                SelectedRankingTag.Value = PickedTags.FirstOrDefault();
                            }
                        }
                    }

                    if (SelectedRankingTag.Value == null)
                    {
                        SelectedRankingTag.Value = PickedTags.FirstOrDefault();
                    }
                }
                else
                {
                    RankingGenre = _previousRankingGenre.Value;
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
                                _notificationService.ShowLiteInAppNotification($"「{selectedTag.Label}」は人気のタグの一覧から外れたようです", Services.LiteNotification.DisplayDuration.MoreAttention);
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

        public override void OnNavigatedFrom(INavigationParameters parameters)
        {
            _IsNavigateCompleted = false;
            _previousRankingGenre = RankingGenre;

            base.OnNavigatedFrom(parameters);
        }


        protected override IIncrementalSource<RankedVideoInfoControlViewModel> GenerateIncrementalSource()
        {
            IsFailedRefreshRanking.Value = false;

            var categoryInfo = RankingGenre;

            IIncrementalSource<RankedVideoInfoControlViewModel> source = null;
            try
            {
                source = new CategoryRankingLoadingSource(RankingGenre, SelectedRankingTag.Value?.Tag, SelectedRankingTerm.Value ?? RankingTerm.Hour, NicoVideoProvider);

                CanChangeRankingParameter.Value = true;
            }
            catch
            {
                IsFailedRefreshRanking.Value = true;
            }            

            return source;
        }

        protected override void PostResetList()
        {
            _IsNavigateCompleted = true;

            base.PostResetList();
        }
    }


    public class CategoryRankingLoadingSource : HohoemaIncrementalSourceBase<RankedVideoInfoControlViewModel>
    {
        public CategoryRankingLoadingSource(
            RankingGenre genre,
            string tag,
            RankingTerm term,
            NicoVideoProvider nicoVideoProvider
            )
            : base()
        {
            Genre = genre;
            Term = term;
            _nicoVideoProvider = nicoVideoProvider;
            Tag = tag;

        }

        public RankingGenre Genre { get; }
        public RankingTerm Term { get; }
        public string Tag { get; }

        Mntone.Nico2.RssVideoResponse RankingRss;
        private readonly NicoVideoProvider _nicoVideoProvider;
        private readonly FeatureFlags _featureFlags = new FeatureFlags();

        #region Implements HohoemaPreloadingIncrementalSourceBase		

        public override uint OneTimeLoadCount => 20;

        protected override async IAsyncEnumerable<RankedVideoInfoControlViewModel> GetPagedItemsImpl(int head, int count, [EnumeratorCancellation] CancellationToken ct = default)
        {
            int index = 0;
            var videoInfoItems = _nicoVideoProvider.GetVideoInfoManyAsync(RankingRss.Items.Skip(head).Take(count).Select(x => x.GetVideoId()));
            await foreach (var item in videoInfoItems)
            {
                var vm = new RankedVideoInfoControlViewModel(item);

                vm.Rank = (uint)(head + index + 1);

                await vm.InitializeAsync(ct).ConfigureAwait(false);

                yield return vm;

                index++;

                ct.ThrowIfCancellationRequested();
            }
        }

        protected override async ValueTask<int> ResetSourceImpl()
        {
            RankingRss = await NiconicoRanking.GetRankingRssAsync(Genre, Tag, Term);

            return RankingRss.Items.Count;

        }


        #endregion




    }


    public class RankedVideoInfoControlViewModel : VideoInfoControlViewModel
    {
        public RankedVideoInfoControlViewModel(
            string rawVideoId
            )
            : base(rawVideoId)
        {

        }

        public RankedVideoInfoControlViewModel(
            NicoVideo data
            )
            : base(data)
        {

        }

        public uint Rank { get; internal set; }
    }
}
