using Mntone.Nico2.Videos.Ranking;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hohoema.Models.Domain;
using System.Reactive.Linq;
using Hohoema.Models.Domain.Helpers;
using System.Text.RegularExpressions;
using Prism.Mvvm;
using Hohoema.Presentation.Services;
using Reactive.Bindings.Extensions;

using Unity;
using Prism.Navigation;
using Hohoema.Presentation.Services.Page;
using Hohoema.Presentation.Services.Helpers;
using Prism.Commands;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reactive.Concurrency;
using Prism.Events;
using Hohoema.Models.UseCase.Playlist;

using I18NPortable;
using Hohoema.Models.UseCase;
using System.Runtime.CompilerServices;
using System.Threading;
using Hohoema.Models.Domain.PageNavigation;
using Hohoema.Models.Domain.Niconico.Video;

namespace Hohoema.Presentation.ViewModels
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

        static Models.Domain.Helpers.AsyncLock _updateLock = new Models.Domain.Helpers.AsyncLock();


        public RankingCategoryPageViewModel(
            ApplicationLayoutManager applicationLayoutManager,
            PageManager pageManager,
            HohoemaPlaylist hohoemaPlaylist,
            NicoVideoProvider nicoVideoProvider,
            RankingProvider rankingProvider,
            VideoRankingSettings rankingSettings,
            IScheduler scheduler,
            IEventAggregator eventAggregator
            )
        {
            ApplicationLayoutManager = applicationLayoutManager;
            PageManager = pageManager;
            HohoemaPlaylist = hohoemaPlaylist;
            NicoVideoProvider = nicoVideoProvider;
            RankingProvider = rankingProvider;
            RankingSettings = rankingSettings;
            _scheduler = scheduler;
            _eventAggregator = eventAggregator;
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
        public RankingProvider RankingProvider { get; }
        
        private static RankingGenre? _previousRankingGenre;
        private readonly IScheduler _scheduler;
        private readonly IEventAggregator _eventAggregator;
        bool _IsNavigateCompleted = false;

        public override async Task OnNavigatedToAsync(INavigationParameters parameters)
        {
            using (await _updateLock.LockAsync())
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

                                _eventAggregator.GetEvent<InAppNotificationEvent>().Publish(new InAppNotificationPayload()
                                {
                                    Content = $"「{selectedTag.Label}」は人気のタグの一覧から外れたようです",
                                    ShowDuration = TimeSpan.FromSeconds(5),
                                    SymbolIcon = Windows.UI.Xaml.Controls.Symbol.Important
                                });
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
                source = new CategoryRankingLoadingSource(RankingGenre, SelectedRankingTag.Value?.Tag, SelectedRankingTerm.Value ?? RankingTerm.Hour);

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
            RankingTerm term
            )
            : base()
        {
            Genre = genre;
            Term = term;
            Tag = tag;
        }

        public RankingGenre Genre { get; }
        public RankingTerm Term { get; }
        public string Tag { get; }

        Mntone.Nico2.RssVideoResponse RankingRss;

        #region Implements HohoemaPreloadingIncrementalSourceBase		

        public override uint OneTimeLoadCount => 20;

        protected override async IAsyncEnumerable<RankedVideoInfoControlViewModel> GetPagedItemsImpl(int head, int count, [EnumeratorCancellation] CancellationToken ct = default)
        {
            int index = 0;
            foreach (var item in RankingRss.Items.Skip(head).Take(count))
            {
                var vm = new RankedVideoInfoControlViewModel(item.GetVideoId());

                vm.Rank = (uint)(head + index + 1);
                /*
                vm.SetTitle(item.GetRankTrimmingTitle());
                var moreData = item.GetMoreData();
                vm.SetVideoDuration(moreData.Length);
                vm.SetThumbnailImage(moreData.ThumbnailUrl);
                vm.SetSubmitDate(item.PubDate.DateTime);
                */
                yield return vm;

                _ = vm.InitializeAsync(ct).ConfigureAwait(false);

                index++;

                ct.ThrowIfCancellationRequested();
            }
        }

        protected override async Task<int> ResetSourceImpl()
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
