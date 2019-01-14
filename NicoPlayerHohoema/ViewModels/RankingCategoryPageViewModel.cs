using Mntone.Nico2.Videos.Ranking;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NicoPlayerHohoema.Models;
using System.Reactive.Linq;
using NicoPlayerHohoema.Models.Helpers;
using System.Text.RegularExpressions;
using Prism.Mvvm;
using NicoPlayerHohoema.Services;
using Reactive.Bindings.Extensions;
using System.Collections.Async;
using NicoPlayerHohoema.Models.Provider;
using Unity;
using Prism.Navigation;
using NicoPlayerHohoema.Services.Page;
using NicoPlayerHohoema.Services.Helpers;

namespace NicoPlayerHohoema.ViewModels
{
    public class RankingCategoryPageViewModel : HohoemaListingPageViewModelBase<RankedVideoInfoControlViewModel>, INavigatedAwareAsync
    {
        public RankingCategoryPageViewModel(
            PageManager pageManager,
            Services.HohoemaPlaylist hohoemaPlaylist,
            NicoVideoProvider nicoVideoProvider,
            RankingSettings rankingSettings,
            NGSettings ngSettings
            )
        {
            PageManager = pageManager;
            HohoemaPlaylist = hohoemaPlaylist;
            NicoVideoProvider = nicoVideoProvider;
            RankingSettings = rankingSettings;
            NgSettings = ngSettings;

            SelectedRankingTarget = RankingSettings.ToReactivePropertyAsSynchronized(x => x.Target, mode: ReactivePropertyMode.DistinctUntilChanged)
                .AddTo(_CompositeDisposable);
            SelectedRankingTimeSpan = RankingSettings.ToReactivePropertyAsSynchronized(x => x.TimeSpan, mode: ReactivePropertyMode.DistinctUntilChanged)
                .AddTo(_CompositeDisposable);

            IsFailedRefreshRanking = new ReactiveProperty<bool>(false)
                .AddTo(_CompositeDisposable);
            CanChangeRankingParameter = new ReactiveProperty<bool>(false)
                .AddTo(_CompositeDisposable);

            new[] {
                SelectedRankingTarget.ToUnit(),
                SelectedRankingTimeSpan.ToUnit()
            }
            .Merge()
            .Subscribe(__ =>
            {
                _ = ResetList();
            })
            .AddTo(_CompositeDisposable);
        }

        private static readonly List<List<RankingCategory>> RankingCategories;
        public static IReadOnlyList<RankingTarget> RankingTargetItems { get; }
        public static IReadOnlyList<RankingTimeSpan> RankingTimeSpanItems { get; }

        static RankingCategoryPageViewModel()
        {
            RankingCategories = new List<List<RankingCategory>>()
            {
                new List<RankingCategory>()
                {
                    RankingCategory.all
                },
                new List<RankingCategory>()
                {
                    RankingCategory.g_ent2,
                    RankingCategory.ent,
                    RankingCategory.music,
                    RankingCategory.sing,
                    RankingCategory.dance,
                    RankingCategory.play,
                    RankingCategory.vocaloid,
                    RankingCategory.nicoindies
                },
                new List<RankingCategory>()
                {
                    RankingCategory.g_life2,
                    RankingCategory.animal,
                    RankingCategory.cooking,
                    RankingCategory.nature,
                    RankingCategory.travel,
                    RankingCategory.sport,
                    RankingCategory.lecture,
                    RankingCategory.drive,
                    RankingCategory.history,
                },
                new List<RankingCategory>()
                {
                    RankingCategory.g_politics
                },
                new List<RankingCategory>()
                {
                    RankingCategory.g_tech,
                    RankingCategory.science,
                    RankingCategory.tech,
                    RankingCategory.handcraft,
                    RankingCategory.make,
                },
                new List<RankingCategory>()
                {
                    RankingCategory.g_culture2,
                    RankingCategory.anime,
                    RankingCategory.game,
                    RankingCategory.jikkyo,
                    RankingCategory.toho,
                    RankingCategory.imas,
                    RankingCategory.radio,
                    RankingCategory.draw,
                },
                new List<RankingCategory>()
                {
                    RankingCategory.g_other,
                    RankingCategory.are,
                    RankingCategory.diary,
                    RankingCategory.other,

                }

            };


            RankingTargetItems = new List<RankingTarget>()
            {
                RankingTarget.fav,
                RankingTarget.view,
                RankingTarget.res,
                RankingTarget.mylist,
            };

            RankingTimeSpanItems = new List<RankingTimeSpan>()
            {
                RankingTimeSpan.hourly,
                RankingTimeSpan.daily,
                RankingTimeSpan.weekly,
                RankingTimeSpan.monthly,
                RankingTimeSpan.total,
            };
        }

        protected override bool TryGetHohoemaPin(out HohoemaPin pin)
        {
            pin = new HohoemaPin()
            {
                Label = RankingCategory.ToCulturelizeString(),
                PageType = HohoemaPageType.RankingCategory,
                Parameter = $"category={RankingCategory}"
            };

            return true;
        }

        public RankingCategory RankingCategory { get; private set; }

        public ReactiveProperty<RankingTarget> SelectedRankingTarget { get; private set; }
        public ReactiveProperty<RankingTimeSpan> SelectedRankingTimeSpan { get; private set; }


        public ReactiveProperty<bool> IsFailedRefreshRanking { get; private set; }
        public ReactiveProperty<bool> CanChangeRankingParameter { get; private set; }
        public PageManager PageManager { get; }
        public Services.HohoemaPlaylist HohoemaPlaylist { get; }
        public NicoVideoProvider NicoVideoProvider { get; }
        public RankingSettings RankingSettings { get; }
        public NGSettings NgSettings { get; }


        private static RankingCategory? _previousRankingCategory;

        public override Task OnNavigatedToAsync(INavigationParameters parameters)
        {
            var mode = parameters.GetNavigationMode();
            if (mode == NavigationMode.New)
            {
                if (parameters.TryGetValue("category", out RankingCategory category))
                {
                    RankingCategory = category;
                }
                else if (parameters.TryGetValue("category", out string categoryString))
                {
                    if (Enum.TryParse(categoryString, out category))
                    {
                        RankingCategory = category;
                    }
                }
                else
                {
                    throw new Exception("ランキングページの表示に失敗");
                }
            }
            else
            {
                RankingCategory = _previousRankingCategory.Value;
            }

            PageManager.PageTitle = RankingCategory.ToCulturelizeString();

            return base.OnNavigatedToAsync(parameters);
        }

        public override void OnNavigatedFrom(INavigationParameters parameters)
        {
            _previousRankingCategory = RankingCategory;

            base.OnNavigatedFrom(parameters);
        }


        protected override IIncrementalSource<RankedVideoInfoControlViewModel> GenerateIncrementalSource()
        {
            IsFailedRefreshRanking.Value = false;

            var categoryInfo = RankingCategory;

            IIncrementalSource<RankedVideoInfoControlViewModel> source = null;
            try
            {
                var target = SelectedRankingTarget.Value;
                var timeSpan = SelectedRankingTimeSpan.Value;
                source = new CategoryRankingLoadingSource(categoryInfo, target, timeSpan, NicoVideoProvider, NgSettings);

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
            base.PostResetList();
        }
    }


    public class CategoryRankingLoadingSource : HohoemaIncrementalSourceBase<RankedVideoInfoControlViewModel>
    {
        public CategoryRankingLoadingSource(
            RankingCategory category, 
            RankingTarget target, 
            RankingTimeSpan timeSpan, 
            NicoVideoProvider nicoVideoProvider, 
            NGSettings ngSettings
            )
            : base()
        {
            Category = category;
            Target = target;
            TimeSpan = timeSpan;
            NicoVideoProvider = nicoVideoProvider;
            NgSettings = ngSettings;
        }

        public RankingCategory Category { get; }
        public RankingTarget Target { get; }
        public RankingTimeSpan TimeSpan { get; }
        public NicoVideoProvider NicoVideoProvider { get; }
        public NGSettings NgSettings { get; }

        NiconicoVideoRss RankingRss;

        readonly Regex RankingRankPrefixPatternRegex = new Regex("(^第\\d*位：)");

        #region Implements HohoemaPreloadingIncrementalSourceBase		

        public override uint OneTimeLoadCount => 100;

        protected override Task<IAsyncEnumerable<RankedVideoInfoControlViewModel>> GetPagedItemsImpl(int head, int count)
        {
            return Task.FromResult(RankingRss.Channel.Items.Skip(head).Take(count)
                .Select((x, index) =>
                {
                    var vm = new RankedVideoInfoControlViewModel(NicoVideoIdHelper.UrlToVideoId(x.VideoUrl));
                    vm.Rank = (uint)(head + index + 1);

                    vm.SetTitle(RankingRankPrefixPatternRegex.Replace(x.Title, ""));
                    return vm;
                })
            .ToAsyncEnumerable()
            );
        }

        protected override async Task<int> ResetSourceImpl()
        {
            RankingRss = await NiconicoRanking.GetRankingData(Target, TimeSpan, Category);

            return RankingRss.Channel.Items.Count;

        }


        #endregion




    }



    public class CategoryWithFav
    {
        public RankingCategory Category { get; set; }
        public bool IsFavorit { get; set; }
    }



    public class RankedVideoInfoControlViewModel : VideoInfoControlViewModel
    {
        public RankedVideoInfoControlViewModel(
            string rawVideoId,
            Interfaces.IMylist ownerPlaylist = null
            )
            : base(rawVideoId, ownerPlaylist)
        {

        }

        public RankedVideoInfoControlViewModel(
            Database.NicoVideo data,
            Interfaces.IMylist ownerPlaylist = null
            )
            : base(data, ownerPlaylist)
        {

        }

        public uint Rank { get; internal set; }
    }


    public class RankingTargetListItem : BindableBase
    {
        public RankingTargetListItem(RankingTarget target)
        {
            TargetType = target;
            Label = target.ToString(); // TODO: RankingTarget のローカライズ
        }

        public string Label { get; private set; }

        public RankingTarget TargetType { get; private set; }
    }


    public class RankingTimeSpanListItem : BindableBase
    {
        public RankingTimeSpanListItem(RankingTimeSpan rankingTimeSpan)
        {
            TimeSpan = rankingTimeSpan;
            Label = rankingTimeSpan.ToString(); //TODO: RankingTimeSpanのローカライズ
        }

        public string Label { get; private set; }

        public RankingTimeSpan TimeSpan { get; private set; }
    }
}
