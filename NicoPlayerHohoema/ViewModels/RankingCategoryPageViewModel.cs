using Mntone.Nico2.Videos.Ranking;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Prism.Windows.Navigation;
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

namespace NicoPlayerHohoema.ViewModels
{
    public class RankingCategoryPageViewModel : HohoemaListingPageViewModelBase<RankedVideoInfoControlViewModel>
    {
        public RankingCategoryPageViewModel(
            PageManager pageManager,
            Services.HohoemaPlaylist hohoemaPlaylist,
            NicoVideoProvider nicoVideoProvider,
            RankingSettings rankingSettings,
            NGSettings ngSettings
            )
            : base(pageManager, useDefaultPageTitle: false)
        {

            HohoemaPlaylist = hohoemaPlaylist;
            NicoVideoProvider = nicoVideoProvider;
            RankingSettings = rankingSettings;
            NgSettings = ngSettings;

            IsFailedRefreshRanking = new ReactiveProperty<bool>(false)
                .AddTo(_CompositeDisposable);
            CanChangeRankingParameter = new ReactiveProperty<bool>(false)
                .AddTo(_CompositeDisposable);

            SelectedRankingTag = new ReactiveProperty<string>();
            SelectedRankingTerm = new ReactiveProperty<RankingTerm>(RankingTerm.Hour);

            /*
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
            */
        }

        static RankingCategoryPageViewModel()
        {
            
        }

        public RankingGenre RankingGenre { get; private set; }

        public ReactiveProperty<string> SelectedRankingTag { get; private set; }
        public ReactiveProperty<RankingTerm> SelectedRankingTerm { get; private set; }


        public ReactiveProperty<bool> IsFailedRefreshRanking { get; private set; }
        public ReactiveProperty<bool> CanChangeRankingParameter { get; private set; }
        public Services.HohoemaPlaylist HohoemaPlaylist { get; }
        public NicoVideoProvider NicoVideoProvider { get; }
        public RankingSettings RankingSettings { get; }
        public NGSettings NgSettings { get; }

        protected override string ResolvePageName()
        {
            return Services.Helpers.CulturelizeHelper.ToCulturelizeString(RankingGenre);
        }

        public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
        {
            if (e.Parameter is string)
            {
                var parameter = e.Parameter as string;
                if (Enum.TryParse(parameter, out RankingGenre genre))
                {
                    RankingGenre = genre;
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

            IIncrementalSource<RankedVideoInfoControlViewModel> source = null;
            try
            {
                source = new CategoryRankingLoadingSource(RankingGenre, SelectedRankingTerm.Value, SelectedRankingTag.Value, NgSettings);

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
            RankingGenre genre,
            RankingTerm? term,
            string tag,
            NGSettings ngSettings
            )
            : base()
        {
            Genre = genre;
            Term = term;
            Tag = tag;
            NgSettings = ngSettings;
        }

        public RankingGenre Genre { get; }
        public RankingTerm? Term { get; }
        public string Tag { get; }
        public NGSettings NgSettings { get; }

        Mntone.Nico2.RssVideoResponse RankingRss;

        readonly Regex RankingRankPrefixPatternRegex = new Regex("(^第\\d*位：)");

        #region Implements HohoemaPreloadingIncrementalSourceBase		

        public override uint OneTimeLoadCount => 100;

        protected override Task<IAsyncEnumerable<RankedVideoInfoControlViewModel>> GetPagedItemsImpl(int head, int count)
        {
            return Task.FromResult(RankingRss.Items.Skip(head).Take(count)
                .Select((x, index) =>
                {
                    var vm = new RankedVideoInfoControlViewModel(x.GetVideoId());

                    vm.Rank = (uint)(head + index + 1);

                    vm.SetTitle(x.GetRankTrimmingTitle());
                    var moreData = x.GetMoreData();
                    vm.SetVideoDuration(moreData.Length);
                    vm.SetThumbnailImage(moreData.ThumbnailUrl);
                    vm.SetSubmitDate(x.PubDate.DateTime);

                    return vm;
                })
            .ToAsyncEnumerable()
            );
        }

        protected override async Task<int> ResetSourceImpl()
        {
            RankingRss = await NiconicoRanking.GetRankingRssAsync(Genre, Tag, Term ?? RankingTerm.Hour);

            return RankingRss.Items.Count;

        }


        #endregion




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
}
