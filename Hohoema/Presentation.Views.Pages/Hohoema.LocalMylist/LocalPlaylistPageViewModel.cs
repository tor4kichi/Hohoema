using Hohoema.Models.UseCase.PageNavigation;
using Hohoema.Models.UseCase;
using Hohoema.Models.UseCase.Playlist;
using Prism.Commands;
using Prism.Navigation;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Hohoema.Models.Domain.PageNavigation;
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Domain.Playlist;
using Hohoema.Models.Domain.Niconico.Mylist.LoginUser;
using Uno.Disposables;
using Hohoema.Presentation.ViewModels.Niconico.Video.Commands;
using Hohoema.Presentation.ViewModels.VideoListPage;
using System.Threading;
using System.Runtime.CompilerServices;
using Hohoema.Models.Helpers;
using Hohoema.Models.Domain.Pins;
using Microsoft.Toolkit.Mvvm.Messaging;
using Microsoft.Toolkit.Collections;
using NiconicoToolkit.Video;
using Hohoema.Models.Domain.LocalMylist;
using Hohoema.Models.UseCase.Hohoema.LocalMylist;
using I18NPortable;
using Reactive.Bindings;

namespace Hohoema.Presentation.ViewModels.Pages.Hohoema.LocalMylist
{
    public sealed class LocalPlaylistPageViewModel : HohoemaListingPageViewModelBase<VideoListItemControlViewModel>, INavigatedAwareAsync, IPinablePage, ITitleUpdatablePage
    {
        HohoemaPin IPinablePage.GetPin()
        {
            return new HohoemaPin()
            {
                Label = Playlist.Name,
                PageType = HohoemaPageType.LocalPlaylist,
                Parameter = $"id={Playlist.PlaylistId.Id}"
            };
        }

        IObservable<string> ITitleUpdatablePage.GetTitleObservable()
        {
            return this.ObserveProperty(x => x.Playlist).Select(x => x?.Name);
        }

        private readonly PageManager _pageManager;
        private readonly LocalMylistManager _localMylistManager;
        private readonly NicoVideoProvider _nicoVideoProvider;
        private readonly IMessenger _messenger;

        public LocalPlaylistPageViewModel(
            IMessenger messenger, 
            ApplicationLayoutManager applicationLayoutManager,
            PageManager pageManager,
            LocalMylistManager localMylistManager,
            NicoVideoProvider nicoVideoProvider,
            VideoPlayWithQueueCommand videoPlayWithQueueCommand,
            PlaylistPlayAllCommand playlistPlayAllCommand,
            LocalPlaylistDeleteCommand localPlaylistDeleteCommand,
            SelectionModeToggleCommand selectionModeToggleCommand            
            )
        {
            ApplicationLayoutManager = applicationLayoutManager;
            _pageManager = pageManager;
            _localMylistManager = localMylistManager;
            _nicoVideoProvider = nicoVideoProvider;
            LocalPlaylistDeleteCommand = localPlaylistDeleteCommand;
            VideoPlayWithQueueCommand = videoPlayWithQueueCommand;
            PlaylistPlayAllCommand = playlistPlayAllCommand;
            SelectionModeToggleCommand = selectionModeToggleCommand;
            _messenger = messenger;

            CurrentPlaylistToken = Observable.CombineLatest(
                this.ObserveProperty(x => x.Playlist),
                this.ObserveProperty(x => x.SelectedSortOptionItem),
                (x, y) => new PlaylistToken(x, y)
                )
                .ToReadOnlyReactivePropertySlim()
                .AddTo(_CompositeDisposable);
                
        }

        public ApplicationLayoutManager ApplicationLayoutManager { get; }

        public LocalPlaylistDeleteCommand LocalPlaylistDeleteCommand { get; }
        public PlaylistPlayAllCommand PlaylistPlayAllCommand { get; }
        public SelectionModeToggleCommand SelectionModeToggleCommand { get; }
        public VideoPlayWithQueueCommand VideoPlayWithQueueCommand { get; }

        private LocalPlaylist _Playlist;
        public LocalPlaylist Playlist
        {
            get { return _Playlist; }
            set { SetProperty(ref _Playlist, value); }
        }


        private LocalPlaylistSortOption _selectedSearchOptionItem;
        public LocalPlaylistSortOption SelectedSortOptionItem
        {
            get { return _selectedSearchOptionItem; }
            set { SetProperty(ref _selectedSearchOptionItem, value); }
        }

        public LocalPlaylistSortOption[] SortOptionItems { get; } = LocalPlaylist.SortOptions;

        public ReadOnlyReactivePropertySlim<PlaylistToken> CurrentPlaylistToken { get; }

        public override async Task OnNavigatedToAsync(INavigationParameters parameters)
        {
            string id = null;

            if (parameters.TryGetValue<string>("id", out var idString))
            {
                id = idString;
            }
            else if (parameters.TryGetValue<int>("id", out var idInt))
            {
                id = idInt.ToString();
            }

            var playlist = _localMylistManager.GetPlaylist(id);

            if (playlist == null) { return; }

            Playlist = playlist;

            SelectedSortOptionItem = SortOptionItems.First(x => x.SortKey == Playlist.ItemsSortKey && x.SortOrder == Playlist.ItemsSortOrder);

            RefreshItems();

            this.ObserveProperty(x => x.SelectedSortOptionItem, isPushCurrentValueAtFirst: false)
                .Subscribe(x => ResetList())
                .AddTo(_NavigatingCompositeDisposable);


            await base.OnNavigatedToAsync(parameters);
        }

        public override void OnNavigatedFrom(INavigationParameters parameters)
        {
            if (Playlist != null)
            {
                WeakReferenceMessenger.Default.Unregister<PlaylistItemRemovedMessage, PlaylistId>(this, Playlist.PlaylistId);
                WeakReferenceMessenger.Default.Unregister<PlaylistItemAddedMessage, PlaylistId>(this, Playlist.PlaylistId);
            }
            base.OnNavigatedFrom(parameters);
        }


        protected override (int, IIncrementalSource<VideoListItemControlViewModel>) GenerateIncrementalSource()
        {
            return (LocalPlaylistIncrementalLoadingSource.OneTimeLoadingCount, new LocalPlaylistIncrementalLoadingSource(Playlist, SelectedSortOptionItem, _nicoVideoProvider));
        }


        void RefreshItems()
        {
            if (Playlist != null)
            {
                WeakReferenceMessenger.Default.Register<PlaylistItemRemovedMessage, PlaylistId>(this, Playlist.PlaylistId, (r, m) => 
                {
                    var args = m.Value;
                    foreach (var video in args.RemovedItems)
                    {
                        var removedItem = ItemsView.Cast<VideoListItemControlViewModel>().FirstOrDefault(x => x.VideoId == video.VideoId);
                        if (removedItem != null)
                        {
                            ItemsView.Remove(removedItem);
                        }
                    }

                    PlaylistPlayAllCommand.RaiseCanExecuteChanged();
                });

                WeakReferenceMessenger.Default.Register<PlaylistItemAddedMessage, PlaylistId>(this, Playlist.PlaylistId, (r, m) =>
                {
                    var args = m.Value;
                    int index = ItemsView.Count;
                    foreach (var video in args.AddedItems)
                    {
                        var nicoVideo = _nicoVideoProvider.GetCachedVideoInfo(video.VideoId);
                        ItemsView.Add(new VideoListItemControlViewModel(nicoVideo) { PlaylistItemToken = new PlaylistItemToken(Playlist, SelectedSortOptionItem, video) });
                    }

                    PlaylistPlayAllCommand.RaiseCanExecuteChanged();
                });

                _localMylistManager.LocalPlaylists.ObserveRemoveChanged()
                    .Subscribe(removed =>
                    {
                        if (Playlist.PlaylistId == removed.PlaylistId)
                        {
                            _pageManager.ForgetLastPage();
                            _pageManager.OpenPage(HohoemaPageType.UserMylist);
                        }
                    })
                    .AddTo(_NavigatingCompositeDisposable);
            }
        }
    }

    public class LocalPlaylistIncrementalLoadingSource : IIncrementalSource<VideoListItemControlViewModel>
    {
        private readonly LocalPlaylist _playlist;
        private readonly LocalPlaylistSortOption _sortOption;
        private readonly NicoVideoProvider _nicoVideoProvider;

        public LocalPlaylistIncrementalLoadingSource(
            LocalPlaylist playlist,
            LocalPlaylistSortOption sortOption,
            NicoVideoProvider nicoVideoProvider
            )
        {
            _playlist = playlist;
            _sortOption = sortOption;
            _nicoVideoProvider = nicoVideoProvider;
        }

        public const int OneTimeLoadingCount = 10;

        List<NicoVideo> _items;
        async Task<IEnumerable<VideoListItemControlViewModel>> IIncrementalSource<VideoListItemControlViewModel>.GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken ct)
        {
            if (pageIndex == 0)
            {
                var items = await _playlist.GetAllItemsAsync(_sortOption, ct);
                _items = items.Cast<NicoVideo>().ToList();
            }
            var head = pageIndex * pageSize;
            

            ct.ThrowIfCancellationRequested();
            return _items.Skip(head).Take(pageSize).Select((item, i) => new VideoListItemControlViewModel(item as NicoVideo) { PlaylistItemToken = new PlaylistItemToken(_playlist, _sortOption, item) });
        }
    }
}
