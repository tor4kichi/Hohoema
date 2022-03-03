using Hohoema.Models.Domain.Application;
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Domain.Niconico.Video.Ranking;
using Hohoema.Models.Domain.PageNavigation;
using Hohoema.Models.UseCase;
using Hohoema.Models.UseCase.PageNavigation;
using Hohoema.Presentation.ViewModels.Niconico.Ranking;
using Hohoema.Presentation.ViewModels.Niconico.Ranking.Messages;
using Hohoema.Presentation.ViewModels.Pages.Niconico.Video;
using I18NPortable;
using Microsoft.Toolkit.Mvvm.Messaging;
using Microsoft.Toolkit.Uwp.UI;
using NiconicoToolkit.Ranking.Video;
using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;
using Hohoema.Presentation.Navigations;

namespace Hohoema.Presentation.ViewModels.Pages.Niconico.VideoRanking
{
    public class RankingCategoryListPageViewModel : HohoemaPageViewModelBase, 
        IRecipient<SettingsRestoredMessage>,
        IRecipient<RankingGenreShowRequestedEvent>,
        IRecipient<RankingGenreHiddenRequestedEvent>,
        IRecipient<RankingGenreFavoriteRequestedEvent>,
        IRecipient<RankingGenreUnFavoriteRequestedEvent>,
        IDisposable
    {
        public RankingCategoryListPageViewModel(
            IMessenger messenger,
            ApplicationLayoutManager applicationLayoutManager,
            PageManager pageManager,
            Services.DialogService dialogService,
            VideoRankingSettings rankingSettings,
            AppFlagsRepository appFlagsRepository,
            RankingProvider rankingProvider
            )
        {
            _messenger = messenger;
            ApplicationLayoutManager = applicationLayoutManager;
            PageManager = pageManager;
            HohoemaDialogService = dialogService;
            RankingSettings = rankingSettings;
            _appFlagsRepository = appFlagsRepository;
            _rankingProvider = rankingProvider;

            WeakReferenceMessenger.Default.Register<SettingsRestoredMessage>(this);

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
                    RankingGenreItems.Add(item);
                }
            }
        }

        void IRecipient<SettingsRestoredMessage>.Receive(SettingsRestoredMessage message)
        {
            FavoriteItems.Clear();
            foreach (var fav in GetFavoriteRankingItems())
            {
                FavoriteItems.Add(fav);
            }

            RefreshFilter();
        }

        void IRecipient<RankingGenreShowRequestedEvent>.Receive(RankingGenreShowRequestedEvent message)
        {
            var args = message.Value;

            // Tagの指定が無い場合はジャンル自体の非表示として扱う
            var genreItem = _RankingGenreItemsSource.First(x => x.Genre == args.RankingGenre);
            if (string.IsNullOrEmpty(args.Tag))
            {
                RankingSettings.RemoveHiddenGenre(args.RankingGenre);

                RankingGenreItems.Clear();
                foreach (var item in _RankingGenreItemsSource)
                {
                    if (item.Genre == null)
                    {
                        RankingGenreItems.Add(item);
                    }
                    else if (!RankingSettings.IsHiddenGenre(item.Genre.Value))
                    {
                        RankingGenreItems.Add(item);
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
        }

        void IRecipient<RankingGenreHiddenRequestedEvent>.Receive(RankingGenreHiddenRequestedEvent message)
        {
            var args = message.Value;

            // Tagの指定が無い場合はジャンル自体の非表示として扱う
            var genreItem = _RankingGenreItemsSource.First(x => x.Genre == args.RankingGenre);
            if (string.IsNullOrEmpty(args.Tag))
            {
                RankingSettings.AddHiddenGenre(args.RankingGenre);

                RankingGenreItems.Clear();
                foreach (var item in _RankingGenreItemsSource)
                {
                    if (item.Genre == null)
                    {
                        RankingGenreItems.Add(item);
                    }
                    else if (!RankingSettings.IsHiddenGenre(item.Genre.Value))
                    {
                        RankingGenreItems.Add(item);
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
        }

        void IRecipient<RankingGenreFavoriteRequestedEvent>.Receive(RankingGenreFavoriteRequestedEvent message)
        {
            var args = message.Value;

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

                var genreGroup = RankingGenreItems.FirstOrDefault(x => x.Genre == args.RankingGenre);
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
        }

        void IRecipient<RankingGenreUnFavoriteRequestedEvent>.Receive(RankingGenreUnFavoriteRequestedEvent message)
        {
            var args = message.Value;

            var unFavoriteItem = FavoriteItems.FirstOrDefault(x => x.Genre == args.RankingGenre && x.Tag == args.Tag);
            if (unFavoriteItem != null)
            {
                FavoriteItems.Remove(unFavoriteItem);
                RankingSettings.RemoveFavoriteTag(unFavoriteItem.Genre.Value, unFavoriteItem.Tag);

                unFavoriteItem.IsFavorite = false;
                System.Diagnostics.Debug.WriteLine($"Favorite removed: {args.RankingGenre} {args.Tag}");
            }

            var genreGroup = RankingGenreItems.FirstOrDefault(x => x.Genre == args.RankingGenre);
            if (genreGroup != null)
            {
                var favItem = genreGroup.Items.Cast<RankingItem>().FirstOrDefault(x => x.Tag == args.Tag);
                if (favItem != null)
                {
                    favItem.IsFavorite = false;
                }
            }
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

        public ObservableCollection<RankingGenreItem> RankingGenreItems { get; } = new ObservableCollection<RankingGenreItem>();
        List<RankingGenreItem> _RankingGenreItemsSource = new List<RankingGenreItem>();

        private RelayCommand<RankingItem> _OpenRankingPageCommand;
        public RelayCommand<RankingItem> OpenRankingPageCommand => _OpenRankingPageCommand
            ?? (_OpenRankingPageCommand = new RelayCommand<RankingItem>(OnRankingCategorySelected));
        
        internal void OnRankingCategorySelected(RankingItem info)
        {
            Debug.WriteLine("OnRankingCategorySelected" + info.Genre);

            var p = new NavigationParameters();
            p.SetRankingGenre(info.Genre.Value);
            p.SetRankingGenreTag(info.Tag);

            _prevSelectedGenre = info.Genre;

            PageManager.OpenPage(HohoemaPageType.RankingCategory, p);
        }

        private RankingGenre? _prevSelectedGenre;
        private readonly IMessenger _messenger;
        private readonly AppFlagsRepository _appFlagsRepository;
        private readonly RankingProvider _rankingProvider;

        public override void OnNavigatedFrom(INavigationParameters parameters)
        {
            base.OnNavigatedFrom(parameters);

            _messenger.Unregister<RankingGenreShowRequestedEvent>(this);
            _messenger.Unregister<RankingGenreHiddenRequestedEvent>(this);
            _messenger.Unregister<RankingGenreFavoriteRequestedEvent>(this);
            _messenger.Unregister<RankingGenreUnFavoriteRequestedEvent>(this);
        }

        public override async void OnNavigatedTo(INavigationParameters parameters)
        {
            _messenger.Register<RankingGenreShowRequestedEvent>(this);
            _messenger.Register<RankingGenreHiddenRequestedEvent>(this);
            _messenger.Register<RankingGenreFavoriteRequestedEvent>(this);
            _messenger.Register<RankingGenreUnFavoriteRequestedEvent>(this);

            if (!_appFlagsRepository.IsRankingInitialUpdate)
            {
                _appFlagsRepository.IsRankingInitialUpdate = true;


                var ct = NavigationCancellationToken;

                try
                {
                    foreach (var genreItem in RankingGenreItems.Cast<RankingGenreItem>())
                    {
                        if (genreItem.Genre == null) { continue; }
                        if (genreItem.Items.Any()) { continue; }

                        var genre = genreItem.Genre.Value;
                        var tags = await _rankingProvider.GetRankingGenreTagsAsync(genre, true, ct);

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

                        ct.ThrowIfCancellationRequested();
                    }
                }
                catch (OperationCanceledException)
                {
                    _appFlagsRepository.IsRankingInitialUpdate = false;
                }
                finally
                {
                }
            }
            else
            {
                if (_prevSelectedGenre != null)
                {
                    var updateTargetGenre = RankingGenreItems.Cast<RankingGenreItem>().FirstOrDefault(x => x.Genre == _prevSelectedGenre);
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
            base.OnNavigatedTo(parameters);
        }

        void RefreshFilter()
        {
            RankingGenreItems.Clear();
            foreach (var item in _RankingGenreItemsSource)
            {
                if (item.Genre == null || !RankingSettings.IsHiddenGenre(item.Genre.Value))
                {
                    RankingGenreItems.Add(item);
                }
            }

            foreach (var genreGroup in RankingGenreItems.Cast<RankingGenreItem>())
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


        RelayCommand _ShowDisplayGenreSelectDialogCommand;
        public RelayCommand ShowDisplayGenreSelectDialogCommand => _ShowDisplayGenreSelectDialogCommand
            ?? (_ShowDisplayGenreSelectDialogCommand = new RelayCommand(async () => 
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

        RelayCommand _ShowDisplayGenreTagSelectDialogCommand;
        public RelayCommand ShowDisplayGenreTagSelectDialogCommand => _ShowDisplayGenreTagSelectDialogCommand
            ?? (_ShowDisplayGenreTagSelectDialogCommand = new RelayCommand(async () =>
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

}
