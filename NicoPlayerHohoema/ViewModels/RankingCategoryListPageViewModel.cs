using Mntone.Nico2.Videos.Ranking;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Prism.Commands;
using NicoPlayerHohoema.Models;
using System.Reactive.Linq;
using Microsoft.Toolkit.Uwp.UI;
using NicoPlayerHohoema.Services.Helpers;
using NicoPlayerHohoema.Services;
using Prism.Navigation;
using NicoPlayerHohoema.Services.Page;

namespace NicoPlayerHohoema.ViewModels
{
    public class RankingCategoryListPageViewModel : HohoemaViewModelBase
    {
        public RankingCategoryListPageViewModel(
            Services.PageManager pageManager,
            Services.DialogService dialogService,
            RankingSettings rankingSettings
            )
        {
            PageManager = pageManager;
            HohoemaDialogService = dialogService;
            RankingSettings = rankingSettings;


            // TODO: ログインユーザーが成年であればR18ジャンルを表示するように
            RankingGenreItems = new ObservableCollection<RankingGenreItem>(
                Enum.GetValues(typeof(RankingGenre)).Cast<RankingGenre>()
                .Where(x => x != RankingGenre.R18)
                .Select(x => new RankingGenreItem()
                {
                    Genre = x,
                    IsDisplay = true
                })
                );
        }

        static RankingCategoryListPageViewModel()
        {
           
        }

        public PageManager PageManager { get; }

        public Services.DialogService HohoemaDialogService { get; }
        public RankingSettings RankingSettings { get; }


        public ObservableCollection<RankingGenreItem> RankingGenreItems { get; private set; }

        private DelegateCommand<RankingItem> _OpenRankingPageCommand;
        public DelegateCommand<RankingItem> OpenRankingPageCommand => _OpenRankingPageCommand
            ?? (_OpenRankingPageCommand = new DelegateCommand<RankingItem>(OnRankingCategorySelected));
        
        internal void OnRankingCategorySelected(RankingItem info)
        {
            var p = new NavigationParameters
            {
                { "genre", info.Genre.ToString() },
                { "tag", info.Tag }
            };
            PageManager.OpenPage(HohoemaPageType.RankingCategory, p);
        }
        


        protected override bool TryGetHohoemaPin(out HohoemaPin pin)
        {
            pin = null;
            return false;
        }
    }


    public class RankingItem
    {
        public RankingGenre Genre { get; set; }
        public string Tag { get; set; }

        public bool IsDisplay { get; set; }
    }

    public class RankingGenreItem : RankingItem
    {
        public List<RankingItem> Items { get; set; }
    }
}
