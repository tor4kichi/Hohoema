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
    public record LocalMylistSortOptionItem(LocalMylistSortKey Key, LocalMylistSortOrder Order, string Label);
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


        private LocalMylistSortOptionItem _selectedSearchOptionItem;
        public LocalMylistSortOptionItem SelectedSearchOptionItem
        {
            get { return _selectedSearchOptionItem; }
            set { SetProperty(ref _selectedSearchOptionItem, value); }
        }

        public LocalMylistSortOptionItem[] SortOptionItems { get; } = new LocalMylistSortOptionItem[] 
        {
            new(LocalMylistSortKey.AddedAt, LocalMylistSortOrder.Desc, "LocalMylistSortKey.AddedAt_Desc".Translate()),
            new(LocalMylistSortKey.AddedAt, LocalMylistSortOrder.Asc, "LocalMylistSortKey.AddedAt_Asc".Translate()),
            new(LocalMylistSortKey.Title, LocalMylistSortOrder.Desc, "LocalMylistSortKey.Title_Desc".Translate()),
            new(LocalMylistSortKey.Title, LocalMylistSortOrder.Asc, "LocalMylistSortKey.Title_Asc".Translate()),
            new(LocalMylistSortKey.PostedAt, LocalMylistSortOrder.Desc, "LocalMylistSortKey.PostedAt_Desc".Translate()),
            new(LocalMylistSortKey.PostedAt, LocalMylistSortOrder.Asc, "LocalMylistSortKey.PostedAt_Asc".Translate()),
        };


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

            SelectedSearchOptionItem = SortOptionItems.First(x => x.Key == Playlist.ItemsSortKey && x.Order == Playlist.ItemsSortOrder);

            RefreshItems();

            this.ObserveProperty(x => x.SelectedSearchOptionItem, isPushCurrentValueAtFirst: false)
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
            Playlist.SetSortOptions(SelectedSearchOptionItem.Key, SelectedSearchOptionItem.Order);
            return (LocalPlaylistIncrementalLoadingSource.OneTimeLoadingCount, new LocalPlaylistIncrementalLoadingSource(Playlist, _nicoVideoProvider));
        }


        void RefreshItems()
        {
            if (Playlist != null)
            {
                WeakReferenceMessenger.Default.Register<PlaylistItemRemovedMessage, PlaylistId>(this, Playlist.PlaylistId, (r, m) => 
                {
                    var args = m.Value;
                    foreach (var itemId in args.RemovedItems)
                    {
                        var removedItem = ItemsView.Cast<VideoListItemControlViewModel>().FirstOrDefault(x => x.VideoId == itemId);
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
                    foreach (var itemId in args.AddedItems)
                    {
                        var video = _nicoVideoProvider.GetCachedVideoInfo(itemId);
                        ItemsView.Add(new VideoListItemControlViewModel(video) { PlaylistItemToken = new PlaylistItemToken(Playlist.PlaylistId, Playlist.SortOptions, itemId) });
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
        private readonly NicoVideoProvider _nicoVideoProvider;

        public LocalPlaylistIncrementalLoadingSource(
            LocalPlaylist playlist,
            NicoVideoProvider nicoVideoProvider
            )
        {
            _playlist = playlist;
            _nicoVideoProvider = nicoVideoProvider;
        }

        public const int OneTimeLoadingCount = 10;

        async Task<IEnumerable<VideoListItemControlViewModel>> IIncrementalSource<VideoListItemControlViewModel>.GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken ct)
        {
            var head = pageIndex * pageSize;
            var targetItems = await _playlist.GetPagedItemsAsync(pageIndex, pageSize, ct);

            ct.ThrowIfCancellationRequested();
            return targetItems.Select(item => new VideoListItemControlViewModel(item as NicoVideo) { PlaylistItemToken = new PlaylistItemToken(_playlist.PlaylistId, _playlist.SortOptions, item.VideoId) });
        }
    }
}
