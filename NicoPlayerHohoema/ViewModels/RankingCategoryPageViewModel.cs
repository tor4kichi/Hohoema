using Mntone.Nico2.Videos.Ranking;
using Prism.Windows.Mvvm;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Windows.Navigation;
using Prism.Commands;
using NicoPlayerHohoema.Models;
using System.Reactive.Linq;
using NicoPlayerHohoema.Helpers;
using System.Text.RegularExpressions;
using Prism.Mvvm;
using NicoPlayerHohoema.Services;
using Reactive.Bindings.Extensions;

namespace NicoPlayerHohoema.ViewModels
{
    public class RankingCategoryPageViewModel : HohoemaVideoListingPageViewModelBase<RankedVideoInfoControlViewModel>
    {
        private static readonly List<List<RankingCategory>> RankingCategories;
        public static IReadOnlyList<RankingTargetListItem> RankingTargetItems { get; }
        public static IReadOnlyList<RankingTimeSpanListItem> RankingTimeSpanItems { get; }

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


            RankingTargetItems = new List<RankingTargetListItem>()
            {
                new RankingTargetListItem(RankingTarget.view),
                new RankingTargetListItem(RankingTarget.res),
                new RankingTargetListItem(RankingTarget.mylist)
            };

            RankingTimeSpanItems = new List<RankingTimeSpanListItem>()
            {
                new RankingTimeSpanListItem(RankingTimeSpan.hourly),
                new RankingTimeSpanListItem(RankingTimeSpan.daily),
                new RankingTimeSpanListItem(RankingTimeSpan.weekly),
                new RankingTimeSpanListItem(RankingTimeSpan.monthly),
                new RankingTimeSpanListItem(RankingTimeSpan.total),
            };
        }



        public RankingCategory RankingCategory { get; private set; }

        public ReactiveProperty<RankingTargetListItem> SelectedRankingTarget { get; private set; }

        public ReactiveProperty<RankingTimeSpanListItem> SelectedRankingTimeSpan { get; private set; }


        public ReactiveProperty<bool> IsFailedRefreshRanking { get; private set; }
        public ReactiveProperty<bool> CanChangeRankingParameter { get; private set; }


        public RankingCategoryPageViewModel(HohoemaApp hohoemaApp, PageManager pageManager)
            : base(hohoemaApp, pageManager)
        {

            SelectedRankingTarget = new ReactiveProperty<RankingTargetListItem>(RankingTargetItems.First());
            SelectedRankingTimeSpan = new ReactiveProperty<RankingTimeSpanListItem>(RankingTimeSpanItems.First());

            IsFailedRefreshRanking = new ReactiveProperty<bool>(false)
                .AddTo(_CompositeDisposable);
            CanChangeRankingParameter = new ReactiveProperty<bool>(false)
                .AddTo(_CompositeDisposable);
        }

        public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
        {
            if (e.Parameter is string)
            {
                var parameter = e.Parameter as string;
                if (Enum.TryParse<RankingCategory>(parameter, out var category))
                {
                    RankingCategory = category;
                }
                else
                {
                    throw new Exception("ランキングページの表示に失敗");
                }
            }

            base.OnNavigatedTo(e, viewModelState);
        }

        public override void OnNavigatingFrom(NavigatingFromEventArgs e, Dictionary<string, object> viewModelState, bool suspending)
        {
            base.OnNavigatingFrom(e, viewModelState, suspending);
        }


        protected override IIncrementalSource<RankedVideoInfoControlViewModel> GenerateIncrementalSource()
        {
            IsFailedRefreshRanking.Value = false;

            var categoryInfo = RankingCategory;

            IIncrementalSource<RankedVideoInfoControlViewModel> source = null;
            try
            {
                var target = SelectedRankingTarget.Value.TargetType;
                var timeSpan = SelectedRankingTimeSpan.Value.TimeSpan;
                source = new CategoryRankingLoadingSource(HohoemaApp, PageManager, categoryInfo, target, timeSpan);

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
            UpdateTitle(Helpers.CulturelizeHelper.ToCulturelizeString(RankingCategory) + "のランキング", RankingCategory.ToString());

            base.PostResetList();
        }
    }


    public class CategoryRankingLoadingSource : HohoemaIncrementalSourceBase<RankedVideoInfoControlViewModel>
    {
        NiconicoVideoRss RankingRss;
        HohoemaApp _HohoemaApp;
        PageManager _PageManager;
        public RankingCategory Category { get; }
        public RankingTarget Target { get; }
        public RankingTimeSpan TimeSpan { get; }


        public CategoryRankingLoadingSource(HohoemaApp app, PageManager pageManager, RankingCategory category, RankingTarget target, RankingTimeSpan timeSpan)
            : base()
        {
            _HohoemaApp = app;
            _PageManager = pageManager;
            Category = category;
            Target = target;
            TimeSpan = timeSpan;
        }


        readonly Regex RankingRankPrefixPatternRegex = new Regex("(^第\\d*位：)");

        #region Implements HohoemaPreloadingIncrementalSourceBase		

        public override uint OneTimeLoadCount => 10;

        protected override Task<IAsyncEnumerable<RankedVideoInfoControlViewModel>> GetPagedItemsImpl(int head, int count)
        {
            return Task.FromResult(RankingRss.Channel.Items.Skip(head).Take(count)
                .Select((x, index) =>
                {
                    var vm = new RankedVideoInfoControlViewModel(
                        (uint)(head + index + 1)
                        , x.GetVideoId()
                    );
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
        public RankedVideoInfoControlViewModel(uint rank, string videoId)
            : base(videoId)
        {
            Rank = rank;
        }



        public uint Rank { get; private set; }
    }


    public class RankingTargetListItem : BindableBase
    {
        public RankingTargetListItem(RankingTarget target)
        {
            TargetType = target;
            Label = target.ToCultulizedText();
        }

        public string Label { get; private set; }

        public RankingTarget TargetType { get; private set; }
    }


    public class RankingTimeSpanListItem : BindableBase
    {
        public RankingTimeSpanListItem(RankingTimeSpan rankingTimeSpan)
        {
            TimeSpan = rankingTimeSpan;
            Label = rankingTimeSpan.ToCultulizedText();
        }

        public string Label { get; private set; }

        public RankingTimeSpan TimeSpan { get; private set; }
    }
}
