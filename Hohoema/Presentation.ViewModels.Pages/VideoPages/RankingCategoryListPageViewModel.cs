using Mntone.Nico2.Videos.Ranking;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Prism.Commands;
using Hohoema.Models.Domain;
using System.Reactive.Linq;
using Microsoft.Toolkit.Uwp.UI;
using Prism.Navigation;
using Hohoema.Presentation.Services.Page;
using Windows.UI.Xaml.Data;
using Prism.Mvvm;
using Prism.Events;
using Reactive.Bindings.Extensions;
using System.Diagnostics;
using Hohoema.Models.UseCase;
using I18NPortable;
using System.Threading.Tasks;
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Domain.PageNavigation;
using Hohoema.Models.Domain.Application;
using Uno.Extensions;

namespace Hohoema.Presentation.ViewModels.Pages.VideoPages
{
    public class RankingCategoryListPageViewModel : HohoemaViewModelBase, INavigatedAwareAsync
    {
        public RankingCategoryListPageViewModel(
            ApplicationLayoutManager applicationLayoutManager,
            PageManager pageManager,
            Services.DialogService dialogService,
            VideoRankingSettings rankingSettings,
            IEventAggregator eventAggregator,
            AppFlagsRepository appFlagsRepository,
            RankingProvider rankingProvider
            )
        {
            ApplicationLayoutManager = applicationLayoutManager;
            PageManager = pageManager;
            HohoemaDialogService = dialogService;
            RankingSettings = rankingSettings;
            _eventAggregator = eventAggregator;
            _appFlagsRepository = appFlagsRepository;
            _rankingProvider = rankingProvider;


            // TODO: ログインユーザーが成年であればR18ジャンルを表示するように
            _RankingGenreItemsSource = new List<RankingGenreItem>();

            FavoriteItems = new ObservableCollection<RankingItem>(GetFavoriteRankingItems());
            _favoriteRankingGenreGroupItem = new FavoriteRankingGenreGroupItem()
            {
                Label = "FavoriteRankingTag".Translate(),
                Items = new AdvancedCollectionView(FavoriteItems)
            };
            _RankingGenreItemsSource.Add(_favoriteRankingGenreGroupItem);
            

            var sourceGenreItems = Enum.GetValues(typeof(RankingGenre)).Cast<RankingGenre>()
                .Where(x => x != RankingGenre.R18)
                .Select(genre =>
                {
                    var rankingItems = ToRankingItem(genre, _rankingProvider.GetRankingGenreTagsFromCache(genre));
                    var acv = new AdvancedCollectionView(new ObservableCollection<RankingItem>(rankingItems), isLiveShaping: true)
                    {
                        Filter = (item) => 
                        {
                            if (item is RankingItem rankingItem && rankingItem.Genre != null)
                            {
                                return !(RankingSettings.IsHiddenGenre(rankingItem.Genre.Value) || RankingSettings.IsHiddenTag(rankingItem.Genre.Value, rankingItem.Tag));
                            }
                            else
                            {
                                return true;
                            }
                        },
                    };

                    return new RankingGenreItem()
                    {
                        Genre = genre,
                        Label = genre.Translate(),
                        Items = acv
                    };
                });

            foreach (var genreItem in sourceGenreItems)
            {
                _RankingGenreItemsSource.Add(genreItem);
            }

            foreach (var item in _RankingGenreItemsSource)
            {
                if (item.Genre == null || !RankingSettings.IsHiddenGenre(item.Genre.Value))
                {
                    _RankingGenreItems.Add(item);
                }
            }

            {
                RankingGenreItems = new CollectionViewSource()
                {
                    Source = _RankingGenreItems,
                    ItemsPath = new Windows.UI.Xaml.PropertyPath(nameof(RankingGenreItem.Items)),
                    IsSourceGrouped = true,
                };
            }

            _eventAggregator.GetEvent<Events.RankingGenreShowRequestedEvent>()
                .Subscribe((args) =>
                {
                    // Tagの指定が無い場合はジャンル自体の非表示として扱う
                    var genreItem = _RankingGenreItemsSource.First(x => x.Genre == args.RankingGenre);
                    if (string.IsNullOrEmpty(args.Tag))
                    {
                        RankingSettings.RemoveHiddenGenre(args.RankingGenre);
                        
                        _RankingGenreItems.Clear();
                        foreach (var item in _RankingGenreItemsSource)
                        {
                            if (item.Genre == null)
                            {
                                _RankingGenreItems.Add(item);
                            }
                            else if (!RankingSettings.IsHiddenGenre(item.Genre.Value))
                            {
                                _RankingGenreItems.Add(item);
                            }
                        }

                        System.Diagnostics.Debug.WriteLine($"Genre Show: {args.RankingGenre}");
                    }
                    else
                    {
                        var sameTagItem = genreItem.Items.SourceCollection.Cast<RankingItem>().FirstOrDefault(x => x.Tag == args.Tag);
                        if (sameTagItem != null)
                        {
                            RankingSettings.RemoveHiddenTag(args.RankingGenre, args.Tag);
                            
                            genreItem.Items.RefreshFilter();

                            System.Diagnostics.Debug.WriteLine($"Tag Show: {args.Tag}");
                        }
                    }
                })
                .AddTo(_CompositeDisposable);

            _eventAggregator.GetEvent<Events.RankingGenreHiddenRequestedEvent>()
                .Subscribe((args) => 
                {
                    // Tagの指定が無い場合はジャンル自体の非表示として扱う
                    var genreItem = _RankingGenreItemsSource.First(x => x.Genre == args.RankingGenre);
                    if (string.IsNullOrEmpty(args.Tag))
                    {
                        RankingSettings.AddHiddenGenre(args.RankingGenre);
                        
                        _RankingGenreItems.Clear();
                        foreach (var item in _RankingGenreItemsSource)
                        {
                            if (item.Genre == null)
                            {
                                _RankingGenreItems.Add(item);
                            }
                            else if (!RankingSettings.IsHiddenGenre(item.Genre.Value))
                            {
                                _RankingGenreItems.Add(item);
                            }
                        }

                        System.Diagnostics.Debug.WriteLine($"Genre Hidden: {args.RankingGenre}");
                    }
                    else
                    {
                        var sameTagItem = genreItem.Items.SourceCollection.Cast<RankingItem>().FirstOrDefault(x => x.Tag == args.Tag);
                        if (sameTagItem != null)
                        {
                            RankingSettings.AddHiddenTag(sameTagItem.Genre.Value, sameTagItem.Tag, sameTagItem.Label);

                            genreItem.Items.RefreshFilter();

                            System.Diagnostics.Debug.WriteLine($"Tag Hidden: {args.Tag}");
                        }
                    }                    
                })
                .AddTo(_CompositeDisposable);

            _eventAggregator.GetEvent<Events.RankingGenreFavoriteRequestedEvent>()
                .Subscribe((args) =>
                {
                    if (false == FavoriteItems.Any(x => x.Genre == args.RankingGenre && x.Tag == args.Tag))
                    {
                        var addedItem = new RankingItem()
                        {
                            Genre = args.RankingGenre,
                            Label = args.Label,
                            IsFavorite = true,
                            Tag = args.Tag
                        };

                        FavoriteItems.Add(addedItem);
                        RankingSettings.AddFavoriteTag(addedItem.Genre.Value, addedItem.Tag, addedItem.Label);

                        var genreGroup = _RankingGenreItems.FirstOrDefault(x => x.Genre == args.RankingGenre);
                        if (genreGroup != null)
                        {
                            var favItem = genreGroup.Items.Cast<RankingItem>().FirstOrDefault(x => x.Tag == args.Tag);
                            if (favItem != null)
                            {
                                favItem.IsFavorite = true;
                            }
                        }

                        System.Diagnostics.Debug.WriteLine($"Favorite added: {args.Label}");
                    }
                })
                .AddTo(_CompositeDisposable);

            _eventAggregator.GetEvent<Events.RankingGenreUnFavoriteRequestedEvent>()
                .Subscribe((args) =>
                {
                    var unFavoriteItem = FavoriteItems.FirstOrDefault(x => x.Genre == args.RankingGenre && x.Tag == args.Tag);
                    if (unFavoriteItem != null)
                    {
                        FavoriteItems.Remove(unFavoriteItem);
                        RankingSettings.RemoveFavoriteTag(unFavoriteItem.Genre.Value, unFavoriteItem.Tag);

                        unFavoriteItem.IsFavorite = false;
                        System.Diagnostics.Debug.WriteLine($"Favorite removed: {args.RankingGenre} {args.Tag}");
                    }

                    var genreGroup = _RankingGenreItems.FirstOrDefault(x => x.Genre == args.RankingGenre);
                    if (genreGroup != null)
                    {
                        var favItem = genreGroup.Items.Cast<RankingItem>().FirstOrDefault(x => x.Tag == args.Tag);
                        if (favItem != null)
                        {
                            favItem.IsFavorite = false;
                        }
                    }
                })
                .AddTo(_CompositeDisposable);
        }

        FavoriteRankingGenreGroupItem _favoriteRankingGenreGroupItem;
        public ObservableCollection<RankingItem> FavoriteItems { get; set; }

        IEnumerable<RankingItem> GetFavoriteRankingItems()
        {
            return RankingSettings.FavoriteTags
                 .Select(y => new RankingItem()
                 {
                     Genre = y.Genre,
                     Label = y.Label,
                     Tag = y.Tag,
                     IsFavorite = true,
                 });
        }


        static RankingCategoryListPageViewModel()
        {
           
        }

        public ApplicationLayoutManager ApplicationLayoutManager { get; }
        public PageManager PageManager { get; }

        public Services.DialogService HohoemaDialogService { get; }
        public VideoRankingSettings RankingSettings { get; }


        public CollectionViewSource RankingGenreItems { get; }
        ObservableCollection<RankingGenreItem> _RankingGenreItems = new ObservableCollection<RankingGenreItem>();
        List<RankingGenreItem> _RankingGenreItemsSource = new List<RankingGenreItem>();

        private DelegateCommand<RankingItem> _OpenRankingPageCommand;
        public DelegateCommand<RankingItem> OpenRankingPageCommand => _OpenRankingPageCommand
            ?? (_OpenRankingPageCommand = new DelegateCommand<RankingItem>(OnRankingCategorySelected));
        
        internal void OnRankingCategorySelected(RankingItem info)
        {
            Debug.WriteLine("OnRankingCategorySelected" + info.Genre);

            var p = new NavigationParameters
            {
                { "genre", info.Genre.ToString() },
                { "tag", info.Tag }
            };
            _prevSelectedGenre = info.Genre;

            PageManager.OpenPage(HohoemaPageType.RankingCategory, p);
        }

        private RankingGenre? _prevSelectedGenre;
        private readonly IEventAggregator _eventAggregator;
        private readonly AppFlagsRepository _appFlagsRepository;
        private readonly RankingProvider _rankingProvider;
        
        public async Task OnNavigatedToAsync(INavigationParameters parameters)
        {
            SettingsRestoredTempraryFlags.Instance.WhenRankingRestored(() =>
            {
                FavoriteItems.Clear();
                FavoriteItems.AddRange(GetFavoriteRankingItems());
                RefreshFilter();
            });


            if (!_appFlagsRepository.IsRankingInitialUpdate)
            {
                _appFlagsRepository.IsRankingInitialUpdate = true;
                try
                {
                    foreach (var genreItem in _RankingGenreItems.Cast<RankingGenreItem>())
                    {
                        if (genreItem.Genre == null) { continue; }
                        if (genreItem.Items.Any()) { continue; }

                        var genre = genreItem.Genre.Value;
                        var tags = await _rankingProvider.GetRankingGenreTagsAsync(genre, true);

                        using (var refresh = genreItem.Items.DeferRefresh())
                        {
                            var rankingItems = tags
                                .Where(x => !string.IsNullOrEmpty(x.Tag) && x.Tag != "all")
                                .Select(y => new RankingItem()
                                {
                                    Genre = genre,
                                    Label = y.Label,
                                    Tag = y.Tag
                                }
                            );
                            genreItem.Items.Clear();
                            foreach (var tag in rankingItems)
                            {
                                genreItem.Items.Add(tag);
                            }
                        }

                        await Task.Delay(500);
                    }
                }
                finally
                {
                }
            }
            else
            {
                if (_prevSelectedGenre != null)
                {
                    var updateTargetGenre = _RankingGenreItems.Cast<RankingGenreItem>().FirstOrDefault(x => x.Genre == _prevSelectedGenre);
                    if (updateTargetGenre != null)
                    {
                        var items = await _rankingProvider.GetRankingGenreTagsAsync(updateTargetGenre.Genre.Value, true);
                        var genreTags = updateTargetGenre.Items.Cast<RankingItem>().Select(x => x.Tag).ToArray();
                        if ((items?.Any() ?? false) && !items.Skip(1).Select(x => x.Tag).SequenceEqual(genreTags))
                        {

                            updateTargetGenre.Items.Clear();
                            using (updateTargetGenre.Items.DeferRefresh())
                            {
                                var genre = updateTargetGenre.Genre.Value;
                                foreach (var a in ToRankingItem(genre, items))
                                {
                                    updateTargetGenre.Items.Add(a);
                                }
                            }
                        }
                    }
                }
            }
        }

        void RefreshFilter()
        {
            _RankingGenreItems.Clear();
            foreach (var item in _RankingGenreItemsSource)
            {
                if (item.Genre == null || !RankingSettings.IsHiddenGenre(item.Genre.Value))
                {
                    _RankingGenreItems.Add(item);
                }
            }

            foreach (var genreGroup in _RankingGenreItems.Cast<RankingGenreItem>())
            {
                genreGroup.Items.RefreshFilter();
            }
        }


        List<RankingItem> ToRankingItem(RankingGenre genre, List<RankingGenreTag> tags)
        {
            return tags
                .Where(x => !string.IsNullOrEmpty(x.Tag) && x.Tag != "all")
                .Select(y => new RankingItem()
                {
                    Genre = genre,
                    Label = y.Label,
                    Tag = y.Tag,
                    IsFavorite = RankingSettings.IsFavoriteTag(genre, y.Tag)
                }
            ).ToList();
        }



        DelegateCommand _ShowDisplayGenreSelectDialogCommand;
        public DelegateCommand ShowDisplayGenreSelectDialogCommand => _ShowDisplayGenreSelectDialogCommand
            ?? (_ShowDisplayGenreSelectDialogCommand = new DelegateCommand(async () => 
            {
                var rankingGenres = Enum.GetValues(typeof(RankingGenre)).Cast<RankingGenre>().Where(x => x != RankingGenre.R18);
                var allItems = rankingGenres.Select(x => new HiddenGenreItem() { Label = x.Translate(), Genre = x }).ToArray();

                var selectedItems = rankingGenres.Where(x => !RankingSettings.HiddenGenres.Contains(x)).Select(x => allItems.First(y => y.Genre == x));

                var result = await HohoemaDialogService.ShowMultiChoiceDialogAsync<HiddenGenreItem>(
                    "SelectDisplayRankingGenre".Translate(), 
                    allItems, selectedItems,
                    nameof(HiddenGenreItem.Label)
                    );

                if (result == null)
                {
                    return;
                }

                var hiddenGenres = rankingGenres.Where(x => !result.Any(y => x == y.Genre));
                RankingSettings.ResetHiddenGenre(hiddenGenres);

                RefreshFilter();
            }));

        DelegateCommand _ShowDisplayGenreTagSelectDialogCommand;
        public DelegateCommand ShowDisplayGenreTagSelectDialogCommand => _ShowDisplayGenreTagSelectDialogCommand
            ?? (_ShowDisplayGenreTagSelectDialogCommand = new DelegateCommand(async () =>
            {
                var result = await HohoemaDialogService.ShowMultiChoiceDialogAsync<RankingGenreTag>(
                    "SelectReDisplayHiddenRankingTags".Translate(),
                    RankingSettings.HiddenTags, Enumerable.Empty<RankingGenreTag>(),
                    nameof(RankingGenreTag.Label)
                    );

                if (result == null) { return; }

                foreach (var removeHiddenTag in result)
                {
                    RankingSettings.RemoveHiddenTag(removeHiddenTag.Genre, removeHiddenTag.Tag);
                }

                RefreshFilter();
            }));
    }


    public class HiddenGenreItem
    {
        public string Label { get; set; }
        public RankingGenre Genre { get; set; }
        public string Tag { get; set; }
    }

    public class RankingItem : BindableBase
    {
        public string Label { get; set; }

        public RankingGenre? Genre { get; set; }
        public string Tag { get; set; }

        private bool _IsFavorite;
        public bool IsFavorite
        {
            get { return _IsFavorite; }
            set { SetProperty(ref _IsFavorite, value); }
        }
    }

    public class RankingGenreItem : RankingItem
    {
        public AdvancedCollectionView Items { get; set; }
    }

    public class FavoriteRankingGenreGroupItem : RankingGenreItem
    {
        
    }
}
