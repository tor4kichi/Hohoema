﻿using Hohoema.Interfaces;
using Hohoema.Models.Helpers;
using Hohoema.Models.Pages;
using Hohoema.Models.Repository.App;
using Hohoema.Models.Repository.Niconico;
using Hohoema.Models.Repository.Niconico.NicoVideo;
using Hohoema.Models.Repository.Niconico.NicoVideo.Ranking;
using Hohoema.UseCase;
using Hohoema.UseCase.Events;
using Hohoema.UseCase.Playlist;
using Hohoema.ViewModels.Pages;
using Hohoema.ViewModels.Pages.Commands;
using Hohoema.ViewModels.Player.Commands;
using I18NPortable;
using Prism.Events;
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

namespace Hohoema.ViewModels
{
    public class RankingCategoryPageViewModel 
        : HohoemaListingPageViewModelBase<RankedVideoInfoControlViewModel>,
        INavigatedAwareAsync,
        IPinablePage,
        ITitleUpdatablePage
    {
        Models.Pages.HohoemaPin IPinablePage.GetPin()
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
            return new Models.Pages.HohoemaPin()
            {
                Label = pickedTag != null ? $"{pickedTag.DisplayName} - {genreName}" : $"{genreName}",
                PageType = HohoemaPageType.RankingCategory,
                Parameter = parameter
            };
        }

        IObservable<string> ITitleUpdatablePage.GetTitleObservable()
        {
            return this.ObserveProperty(x => x.RankingGenre)
                .Select(genre => "RankingTitleWithGenre".Translate(genre.Translate()));
        }

        static Models.Helpers.AsyncLock _updateLock = new Models.Helpers.AsyncLock();


        public RankingCategoryPageViewModel(
            ApplicationLayoutManager applicationLayoutManager,
            PageManager pageManager,
            HohoemaPlaylist hohoemaPlaylist,
            NicoVideoProvider nicoVideoProvider,
            RankingProvider rankingProvider,
            RankingSettingsRepository rankingSettings,
            IScheduler scheduler,
            IEventAggregator eventAggregator,
            OpenPageCommand openPageCommand,
            PlayVideoCommand playVideoCommand
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
            OpenPageCommand = openPageCommand;
            PlayVideoCommand = playVideoCommand;
            IsFailedRefreshRanking = new ReactiveProperty<bool>(false)
                .AddTo(_CompositeDisposable);
            CanChangeRankingParameter = new ReactiveProperty<bool>(false)
                .AddTo(_CompositeDisposable);

            SelectedRankingTag = new ReactiveProperty<Database.Local.RankingGenreTag>();
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
                        return RankingConstants.AllRankingTerms;
                    }
                    else
                    {
                        return RankingConstants.GenreWithTagAccepteRankingTerms;
                    }
                }
                else
                {
                    return RankingConstants.HotTopicAccepteRankingTerms;
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

        public ReactiveProperty<Database.Local.RankingGenreTag> SelectedRankingTag { get; private set; }
        public ReactiveProperty<RankingTerm?> SelectedRankingTerm { get; private set; }

        public IReadOnlyReactiveProperty<RankingTerm[]> CurrentSelectableRankingTerms { get; }

        public ObservableCollection<Database.Local.RankingGenreTag> PickedTags { get; } = new ObservableCollection<Database.Local.RankingGenreTag>();


        public ReactiveProperty<bool> IsFailedRefreshRanking { get; private set; }
        public ReactiveProperty<bool> CanChangeRankingParameter { get; private set; }
        public ApplicationLayoutManager ApplicationLayoutManager { get; }
        public PageManager PageManager { get; }
        public HohoemaPlaylist HohoemaPlaylist { get; }
        public NicoVideoProvider NicoVideoProvider { get; }
        public RankingSettingsRepository RankingSettings { get; }
        public RankingProvider RankingProvider { get; }

        public OpenPageCommand OpenPageCommand { get; }
        public PlayVideoCommand PlayVideoCommand { get; }

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

                        // 好きなジャンルタグ情報から消されたタグを除去する
                        RankingSettings.FavoriteTags = RankingSettings.FavoriteTags
                            .Where(oldFavTag => oldFavTag.Genre != RankingGenre || PickedTags.Any(x => x.Tag == oldFavTag.Tag))
                            .ToArray();

                        var selectedTag = SelectedRankingTag.Value;
                        if (selectedTag.Tag != null)
                        {
                            if (false == PickedTags.Any(x => x.Tag == selectedTag.Tag))
                            {
                                SelectedRankingTag.Value = PickedTags.ElementAtOrDefault(0);

                                _eventAggregator.GetEvent<InAppNotificationEvent>().Publish(new InAppNotificationPayload()
                                {
                                    Content = $"「{selectedTag.DisplayName}」は人気のタグの一覧から外れたようです",
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
                source = new CategoryRankingLoadingSource(RankingProvider, RankingGenre, SelectedRankingTag.Value?.Tag, SelectedRankingTerm.Value ?? RankingTerm.Hour);

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
            RankingProvider rankingProvider,
            RankingGenre genre,
            string tag,
            RankingTerm term
            )
            : base()
        {
            _rankingProvider = rankingProvider;
            Genre = genre;
            Term = term;
            Tag = tag;
        }

        public RankingGenre Genre { get; }
        public RankingTerm Term { get; }
        public string Tag { get; }

        RssVideoResponse RankingRss;
        private readonly RankingProvider _rankingProvider;

        #region Implements HohoemaPreloadingIncrementalSourceBase		

        public override uint OneTimeLoadCount => 20;

        protected override async IAsyncEnumerable<RankedVideoInfoControlViewModel> GetPagedItemsImpl(int head, int count, [EnumeratorCancellation]CancellationToken cancellationToken)
        {
            int index = 0;
            foreach (var item in RankingRss.Items.Skip(head).Take(count))
            {
                var vm = new RankedVideoInfoControlViewModel(item.VideoId);
                vm.Rank = (uint)(head + index + 1);
                await vm.InitializeAsync(cancellationToken);
                yield return vm;

                index++;
            }
        }

        protected override async Task<int> ResetSourceImpl()
        {
            RankingRss = await _rankingProvider.GetRankingGenreWithTagAsync(Genre, Tag, Term);

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
            Database.NicoVideo data
            )
            : base(data)
        {

        }

        public uint Rank { get; internal set; }
    }
}
