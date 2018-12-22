using Mntone.Nico2.Videos.Ranking;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Prism.Windows.Navigation;
using Prism.Commands;
using NicoPlayerHohoema.Models;
using System.Reactive.Linq;
using Microsoft.Toolkit.Uwp.UI;
using NicoPlayerHohoema.Services.Helpers;
using NicoPlayerHohoema.Services;

namespace NicoPlayerHohoema.ViewModels
{
    public class RankingCategoryListPageViewModel : HohoemaViewModelBase
    {
        public RankingCategoryListPageViewModel(
            Services.PageManager pageManager, 
            Services.DialogService dialogService,
            RankingSettings rankingSettings
            )
            : base(pageManager)
        {
            HohoemaDialogService = dialogService;
            RankingSettings = rankingSettings;


            Func<RankingCategory, bool> checkFavorite = (RankingCategory cat) =>
            {
                return RankingSettings.HighPriorityCategory.Any(x => x.Category == cat);
            };


            RankingCategoryItems = new ObservableCollection<RankingCategoryHostListItem>();
            FavoriteRankingCategoryItems = new ObservableCollection<RankingCategoryListPageListItem>();

            SelectedRankingCategory = new ReactiveProperty<RankingCategoryListPageListItem>();

            AddFavRankingCategory = new DelegateCommand(async () =>
            {
                var items = new AdvancedCollectionView();
                items.SortDescriptions.Add(new SortDescription("IsFavorit", SortDirection.Descending));
                items.SortDescriptions.Add(new SortDescription("Category", SortDirection.Ascending));

                var highPriCat = RankingSettings.HighPriorityCategory;
                var lowPriCat = RankingSettings.LowPriorityCategory;
                foreach (var i in RankingSettings.HighPriorityCategory)
                {
                    items.Add(new CategoryWithFav() { Category = i.Category, IsFavorit = true });
                }

                var middleRankingCategories = Enum.GetValues(typeof(RankingCategory)).Cast<RankingCategory>()
                    .Where(x => !highPriCat.Any(h => x == h.Category))
                    .Where(x => !lowPriCat.Any(l => x == l.Category))
                    ;
                foreach (var category in middleRankingCategories)
                {
                    items.Add(new CategoryWithFav() { Category = category });
                }
                items.Refresh();

                var choiceItems = await HohoemaDialogService.ShowMultiChoiceDialogAsync(
                    "優先表示にするカテゴリを選択",
                    items.Cast<CategoryWithFav>().Select(x => new RankingCategoryInfo(x.Category)),
                    RankingSettings.HighPriorityCategory.ToArray(),
                    x => x.DisplayLabel
                    );

                if (choiceItems == null) { return; }

                // choiceItemsに含まれるカテゴリをMiddleとLowから削除
                RankingSettings.ResetFavoriteCategory();

                // HighにchoiceItemsを追加（重複しないよう注意）
                foreach (var cat in choiceItems)
                {
                    RankingSettings.AddFavoritCategory(cat.Category);
                }

                await RankingSettings.Save();

                ResetRankingCategoryItems();
            });



            AddDislikeRankingCategory = new DelegateCommand(async () =>
            {
                var items = new AdvancedCollectionView();
                items.SortDescriptions.Add(new SortDescription("IsFavorit", SortDirection.Descending));
                items.SortDescriptions.Add(new SortDescription("Category", SortDirection.Ascending));

                var highPriCat = RankingSettings.HighPriorityCategory;
                var lowPriCat = RankingSettings.LowPriorityCategory;
                foreach (var i in lowPriCat)
                {
                    items.Add(new CategoryWithFav() { Category = i.Category, IsFavorit = true });
                }
                var middleRankingCategories = Enum.GetValues(typeof(RankingCategory)).Cast<RankingCategory>()
                    .Where(x => !highPriCat.Any(h => x == h.Category))
                    .Where(x => !lowPriCat.Any(l => x == l.Category))
                    ;
                foreach (var category in middleRankingCategories)
                {
                    items.Add(new CategoryWithFav() { Category = category });
                }
                items.Refresh();

                var choiceItems = await HohoemaDialogService.ShowMultiChoiceDialogAsync(
                    "非表示にするカテゴリを選択",
                    items.Cast<CategoryWithFav>().Select(x => new RankingCategoryInfo(x.Category)),
                    RankingSettings.LowPriorityCategory,
                    x => x.DisplayLabel
                    );

                if (choiceItems == null) { return; }

                // choiceItemsに含まれるカテゴリをMiddleとLowから削除
                RankingSettings.ResetDislikeCategory();

                // HighにchoiceItemsを追加（重複しないよう注意）
                foreach (var cat in choiceItems)
                {
                    RankingSettings.AddDislikeCategory(cat.Category);
                }

                await RankingSettings.Save();

                ResetRankingCategoryItems();
            });

            ResetRankingCategoryItems();
        }

        static private readonly List<List<RankingCategory>> RankingCategories;

        static RankingCategoryListPageViewModel()
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
                    RankingCategory.nicoindies,
                    RankingCategory.asmr,
                    RankingCategory.mmd,
                    RankingCategory.Virtual,
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
                    RankingCategory.train,
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
                    RankingCategory.trpg,
                },
                new List<RankingCategory>()
                {
                    RankingCategory.g_other,
                    RankingCategory.are,
                    RankingCategory.diary,
                    RankingCategory.other,

                }

            };

        }


        public Services.DialogService HohoemaDialogService { get; }
        public RankingSettings RankingSettings { get; }


        public ReactiveProperty<RankingCategoryListPageListItem> SelectedRankingCategory { get; }

        public ObservableCollection<RankingCategoryListPageListItem> FavoriteRankingCategoryItems { get; private set; }
        public ObservableCollection<RankingCategoryHostListItem> RankingCategoryItems { get; private set; }


        public DelegateCommand AddFavRankingCategory { get; private set; }
        public DelegateCommand AddDislikeRankingCategory { get; private set; }





        public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
        {
            base.OnNavigatedTo(e, viewModelState);
        }


        void ResetRankingCategoryItems()
        {
            RankingCategoryItems.Clear();

            RankingCategoryItems.Add(
                new RankingCategoryHostListItem("好きなランキング")
                {
                    ChildItems = RankingSettings.HighPriorityCategory
                        .Select(x => new RankingCategoryListPageListItem(x.Category, true, OnRankingCategorySelected))
                        .ToList()
                }
                );
            foreach (var categoryList in RankingCategories)
            {
                // 非表示ランキングを除外したカテゴリリストを作成
                var label = categoryList.First().ToCulturelizeString();

                var list = categoryList
                    .Where(x => !RankingSettings.IsDislikeRankingCategory(x))
                    .Select(x => CreateRankingCategryListItem(x))
                    .ToList();

                // 表示対象があればリストに追加
                if (list.Count > 0)
                {
                    RankingCategoryItems.Add(new RankingCategoryHostListItem(label) { ChildItems = list });
                }
            }

            RaisePropertyChanged(nameof(RankingCategoryItems));
        }

        internal void OnRankingCategorySelected(RankingCategory info)
        {
            PageManager.OpenPage(HohoemaPageType.RankingCategory, info.ToString());
        }



        RankingCategoryListPageListItem CreateRankingCategryListItem(RankingCategory category)
        {
            var categoryInfo = RankingCategoryInfo.CreateFromRankingCategory(category);
            var isFavoriteCategory = RankingSettings.HighPriorityCategory.Contains(categoryInfo);
            return new RankingCategoryListPageListItem(category, isFavoriteCategory, OnRankingCategorySelected);
        }
    }



    public class RankingCategoryHostListItem
    {
        public string Label { get; }
        public RankingCategoryHostListItem(string label)
        {
            Label = label;
            ChildItems = new List<RankingCategoryListPageListItem>();
            SelectedCommand = new DelegateCommand<RankingCategoryListPageListItem>((item) =>
            {
                item.PrimaryCommand.Execute(null);
            });

        }

        public bool HasItem => ChildItems.Count > 0;


        public List<RankingCategoryListPageListItem> ChildItems { get; set; }


        public DelegateCommand<RankingCategoryListPageListItem> SelectedCommand { get; }
    }


    public class RankingCategoryListPageListItem : SelectableItem<RankingCategory>
    {
        public bool IsFavorite { get; private set; }

        public RankingCategoryListPageListItem(RankingCategory category, bool isFavoriteCategory, Action<RankingCategory> selected)
            : base(category, selected)
        {
            IsFavorite = isFavoriteCategory;
        }
    }
}
