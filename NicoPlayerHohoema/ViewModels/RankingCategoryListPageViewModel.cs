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
using Reactive.Bindings.Extensions;
using Windows.UI.Xaml.Navigation;
using Prism.Mvvm;
using System.Text.RegularExpressions;
using NicoPlayerHohoema.Views.Service;
using Microsoft.Toolkit.Uwp.UI;

namespace NicoPlayerHohoema.ViewModels
{
	public class RankingCategoryListPageViewModel : HohoemaVideoListingPageViewModelBase<RankedVideoInfoControlViewModel>
	{

        public static IReadOnlyList<RankingCategory> RankingCategories { get; }
        public static IReadOnlyList<RankingTargetListItem> RankingTargetItems { get; }
        public static IReadOnlyList<RankingTimeSpanListItem> RankingTimeSpanItems { get; }

        static RankingCategoryListPageViewModel()
		{
			RankingCategories = new List<RankingCategory>()
			{
				RankingCategory.all,

				RankingCategory.g_ent2,
				RankingCategory.ent,
				RankingCategory.music,
				RankingCategory.sing,
				RankingCategory.dance,
				RankingCategory.play,
				RankingCategory.vocaloid,
				RankingCategory.nicoindies,

				RankingCategory.g_life2,
				RankingCategory.animal,
				RankingCategory.cooking,
				RankingCategory.nature,
				RankingCategory.travel,
				RankingCategory.sport,
				RankingCategory.lecture,
				RankingCategory.drive,
				RankingCategory.history,

                RankingCategory.g_politics,

                RankingCategory.g_tech,
				RankingCategory.science,
				RankingCategory.tech,
				RankingCategory.handcraft,
				RankingCategory.make,

                RankingCategory.g_culture2,
				RankingCategory.anime,
				RankingCategory.game,
				RankingCategory.jikkyo,
				RankingCategory.toho,
				RankingCategory.imas,
				RankingCategory.radio,
				RankingCategory.draw,

                RankingCategory.g_other,
				RankingCategory.are,
				RankingCategory.diary,
				RankingCategory.other,
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


        static RankingCategory? _LastSelectedRankingCategory;

        Services.HohoemaDialogService _HohoemaDialogService;




        public ReactiveProperty<CategoryWithFav> SelectedRankingCategory { get; }

        public AdvancedCollectionView SortedRankingCategoryItems { get; private set; }




        public ReactiveProperty<RankingTargetListItem> SelectedRankingTarget { get; private set; }

        public ReactiveProperty<RankingTimeSpanListItem> SelectedRankingTimeSpan { get; private set; }


        public ReactiveProperty<bool> IsFailedRefreshRanking { get; private set; }
        public ReactiveProperty<bool> CanChangeRankingParameter { get; private set; }


        RankingSettings _RankingSettings;


        private DelegateCommand<RankingCategory?> _AddFavoritCategoryCommand;
        public DelegateCommand<RankingCategory?> AddFavoritCategoryCommand
        {
            get
            {
                return _AddFavoritCategoryCommand
                    ?? (_AddFavoritCategoryCommand = new DelegateCommand<RankingCategory?>((category) => 
                    {
                        if (category.HasValue)
                        {
                            var catItem = _RankingSettings.MiddlePriorityCategory.FirstOrDefault(x => x.Category == category.Value);
                            if (catItem != null)
                            {
                                _RankingSettings.MiddlePriorityCategory.Remove(catItem);
                                _RankingSettings.HighPriorityCategory.Add(catItem);
                            }
                        }
                    }
                    ));
            }
        }


        private DelegateCommand<RankingCategory?> _AddHiddenCategoryCommand;
        public DelegateCommand<RankingCategory?> AddHiddenCategoryCommand
        {
            get
            {
                return _AddHiddenCategoryCommand
                    ?? (_AddHiddenCategoryCommand = new DelegateCommand<RankingCategory?>((category) =>
                    {
                        if (category.HasValue)
                        {
                            var catItem = _RankingSettings.MiddlePriorityCategory.FirstOrDefault(x => x.Category == category.Value);
                            if (catItem != null)
                            {
                                _RankingSettings.MiddlePriorityCategory.Remove(catItem);
                                _RankingSettings.LowPriorityCategory.Add(catItem);
                            }
                        }
                    }
                    ));
            }
        }


        private DelegateCommand _ResetCategoryCommand;
        public DelegateCommand ResetCategoryCommand
        {
            get
            {
                return _ResetCategoryCommand
                    ?? (_ResetCategoryCommand = new DelegateCommand(() =>
                    {
                        _RankingSettings.ResetCategoryPriority();
                    }
                    ));
            }
        }


        private DelegateCommand<RankingCategory?> _UnselectCategoryCommand;
        public DelegateCommand<RankingCategory?> UnselectCategoryCommand
        {
            get
            {
                return _UnselectCategoryCommand
                    ?? (_UnselectCategoryCommand = new DelegateCommand<RankingCategory?>((category) =>
                    {
                        SelectedRankingCategory.Value = null;   
                    }
                    ));
            }
        }

        public DelegateCommand AddFavRankingCategory { get; private set; }
        public DelegateCommand AddDislikeRankingCategory { get; private set; }


        public RankingCategoryListPageViewModel(
            HohoemaApp hohoemaApp, 
            PageManager pageManager,
            Services.HohoemaDialogService dialogService
            )
			: base(hohoemaApp, pageManager)
		{
            _HohoemaDialogService = dialogService;
            _RankingSettings = HohoemaApp.UserSettings.RankingSettings;

            IsFailedRefreshRanking = new ReactiveProperty<bool>(false)
                .AddTo(_CompositeDisposable);
            CanChangeRankingParameter = new ReactiveProperty<bool>(false)
                .AddTo(_CompositeDisposable);

            SelectedRankingCategory = new ReactiveProperty<CategoryWithFav>(mode:ReactivePropertyMode.DistinctUntilChanged)
                .AddTo(_CompositeDisposable);

            SelectedRankingCategory.Subscribe(async x => 
            {
                if (x != null)
                {
                    var currentCategory = (IncrementalLoadingItems?.Source as CategoryRankingLoadingSource)?.Category;
                    if (currentCategory != x.Category)
                    {
                        await ResetList();
                    }
                }
            })
            .AddTo(_CompositeDisposable);

            SelectedRankingTarget = new ReactiveProperty<RankingTargetListItem>(RankingTargetItems[0], ReactivePropertyMode.DistinctUntilChanged)
                .AddTo(_CompositeDisposable);
            SelectedRankingTimeSpan = new ReactiveProperty<RankingTimeSpanListItem>(RankingTimeSpanItems[0], ReactivePropertyMode.DistinctUntilChanged)
                .AddTo(_CompositeDisposable);

            Observable.Merge(
                SelectedRankingTimeSpan.ToUnit(),
                SelectedRankingTarget.ToUnit()
                )
                .SubscribeOnUIDispatcher()
                .Subscribe(async x =>
                {
                    await ResetList();
                })
                .AddTo(_CompositeDisposable);


            SortedRankingCategoryItems = new AdvancedCollectionView();
            SortedRankingCategoryItems.SortDescriptions.Add(new SortDescription("IsFavorit", SortDirection.Descending));
            SortedRankingCategoryItems.SortDescriptions.Add(new SortDescription("Category", SortDirection.Ascending));
            
            foreach (var i in HohoemaApp.UserSettings.RankingSettings.HighPriorityCategory)
            {
                SortedRankingCategoryItems.Add(new CategoryWithFav() { Category = i.Category, IsFavorit = true });
            }
            foreach (var i in HohoemaApp.UserSettings.RankingSettings.MiddlePriorityCategory)
            {
                SortedRankingCategoryItems.Add(new CategoryWithFav() { Category = i.Category});
            }
            SortedRankingCategoryItems.Refresh();

            Observable.Merge(
                HohoemaApp.UserSettings.RankingSettings.MiddlePriorityCategory.CollectionChangedAsObservable().ToUnit(),
                HohoemaApp.UserSettings.RankingSettings.HighPriorityCategory.CollectionChangedAsObservable().ToUnit()
                )
                .Throttle(TimeSpan.FromMilliseconds(250))
                .Subscribe(async x => 
                {
                    await HohoemaApp.UIDispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => 
                    {
                        var selectedItem = SelectedRankingCategory.Value;

                        using (var releaser = SortedRankingCategoryItems.DeferRefresh())
                        {
                            SortedRankingCategoryItems.Clear();
                            foreach (var i in HohoemaApp.UserSettings.RankingSettings.HighPriorityCategory)
                            {
                                SortedRankingCategoryItems.Add(new CategoryWithFav() { Category = i.Category, IsFavorit = true });
                            }
                            foreach (var i in HohoemaApp.UserSettings.RankingSettings.MiddlePriorityCategory)
                            {
                                SortedRankingCategoryItems.Add(new CategoryWithFav() { Category = i.Category });
                            }
                        }

                        SelectedRankingCategory.Value = selectedItem;
                    });
                })
            .AddTo(_CompositeDisposable);

            AddFavRankingCategory = new DelegateCommand(async () =>
            {
                var items = new AdvancedCollectionView();
                items.SortDescriptions.Add(new SortDescription("IsFavorit", SortDirection.Descending));
                items.SortDescriptions.Add(new SortDescription("Category", SortDirection.Ascending));

                foreach (var i in HohoemaApp.UserSettings.RankingSettings.HighPriorityCategory)
                {
                    items.Add(new CategoryWithFav() { Category = i.Category, IsFavorit = true });
                }
                foreach (var i in HohoemaApp.UserSettings.RankingSettings.MiddlePriorityCategory)
                {
                    items.Add(new CategoryWithFav() { Category = i.Category });
                }
                items.Refresh();

                var choiceItems = await _HohoemaDialogService.ShowMultiChoiceDialogAsync(
                    "優先表示にするカテゴリを選択",
                    items.Cast<CategoryWithFav>().Select(x => new RankingCategoryInfo(x.Category)),
                    _RankingSettings.HighPriorityCategory.ToArray(),
                    x => x.DisplayLabel
                    );

                if (choiceItems == null) { return; }

                // choiceItemsに含まれるカテゴリをMiddleとLowから削除
                _RankingSettings.ResetFavoriteCategory();

                // HighにchoiceItemsを追加（重複しないよう注意）
                foreach (var cat in choiceItems)
                {
                    _RankingSettings.AddFavoritCategory(cat.Category);
                }
            });
            


            AddDislikeRankingCategory = new DelegateCommand(async () =>
            {
                var items = new AdvancedCollectionView();
                items.SortDescriptions.Add(new SortDescription("IsFavorit", SortDirection.Descending));
                items.SortDescriptions.Add(new SortDescription("Category", SortDirection.Ascending));

                foreach (var i in HohoemaApp.UserSettings.RankingSettings.LowPriorityCategory)
                {
                    items.Add(new CategoryWithFav() { Category = i.Category, IsFavorit = true });
                }
                foreach (var i in HohoemaApp.UserSettings.RankingSettings.MiddlePriorityCategory)
                {
                    items.Add(new CategoryWithFav() { Category = i.Category });
                }
                items.Refresh();

                var choiceItems = await _HohoemaDialogService.ShowMultiChoiceDialogAsync(
                    "非表示にするカテゴリを選択",
                    items.Cast<CategoryWithFav>().Select(x => new RankingCategoryInfo(x.Category)),
                    _RankingSettings.LowPriorityCategory,
                    x => x.DisplayLabel
                    );

                if (choiceItems == null) { return; }

                // choiceItemsに含まれるカテゴリをMiddleとLowから削除
                _RankingSettings.ResetDislikeCategory();

                // HighにchoiceItemsを追加（重複しないよう注意）
                foreach (var cat in choiceItems)
                {
                    _RankingSettings.AddDislikeCategory(cat.Category);
                }                
            });
        }

        public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
            if (_LastSelectedRankingCategory.HasValue)
            {
                SelectedRankingCategory.Value = SortedRankingCategoryItems.Cast<CategoryWithFav>().SingleOrDefault(x => x.Category == _LastSelectedRankingCategory.Value);
            }

            base.OnNavigatedTo(e, viewModelState);
		}

        public override void OnNavigatingFrom(NavigatingFromEventArgs e, Dictionary<string, object> viewModelState, bool suspending)
        {
            _LastSelectedRankingCategory = SelectedRankingCategory.Value?.Category;

            base.OnNavigatingFrom(e, viewModelState, suspending);
        }

        protected override bool CheckNeedUpdateOnNavigateTo(NavigationMode mode)
        {
            if (SelectedRankingCategory.Value == null)
            {
                return false;
            }
            else
            {
                //return mode == NavigationMode.New;

                // MasterDetailsView側でSelectedItemの再設定が行われるので
                return false;
            }

//            return base.CheckNeedUpdateOnNavigateTo(mode);
        }

        protected override IIncrementalSource<RankedVideoInfoControlViewModel> GenerateIncrementalSource()
        {
            IsFailedRefreshRanking.Value = false;

            var categoryInfo = SelectedRankingCategory.Value;
            
            if (categoryInfo == null) { return null; }

            IIncrementalSource<RankedVideoInfoControlViewModel> source = null;
            try
            {
                var target = SelectedRankingTarget.Value.TargetType;
                var timeSpan = SelectedRankingTimeSpan.Value.TimeSpan;
                source = new CategoryRankingLoadingSource(HohoemaApp, PageManager, categoryInfo.Category, target, timeSpan);

                CanChangeRankingParameter.Value = true;
            }
            catch
            {
                IsFailedRefreshRanking.Value = true;
            }


            return source;
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
