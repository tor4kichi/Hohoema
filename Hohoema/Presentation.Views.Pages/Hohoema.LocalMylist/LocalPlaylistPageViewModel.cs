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
            ApplicationLayoutManager applicationLayoutManager,
            PageManager pageManager,
            LocalMylistManager localMylistManager,
            NicoVideoProvider nicoVideoProvider,
            LocalPlaylistDeleteCommand localPlaylistDeleteCommand,
            PlaylistPlayAllCommand playlistPlayAllCommand,
            SelectionModeToggleCommand selectionModeToggleCommand,
            IMessenger messenger
            )
        {
            ApplicationLayoutManager = applicationLayoutManager;
            _pageManager = pageManager;
            _localMylistManager = localMylistManager;
            _nicoVideoProvider = nicoVideoProvider;
            LocalPlaylistDeleteCommand = localPlaylistDeleteCommand;
            PlaylistPlayAllCommand = playlistPlayAllCommand;
            SelectionModeToggleCommand = selectionModeToggleCommand;
            _messenger = messenger;
        }

        public ApplicationLayoutManager ApplicationLayoutManager { get; }

        public LocalPlaylistDeleteCommand LocalPlaylistDeleteCommand { get; }
        public PlaylistPlayAllCommand PlaylistPlayAllCommand { get; }
        public SelectionModeToggleCommand SelectionModeToggleCommand { get; }

        private LocalPlaylist _Playlist;
        public LocalPlaylist Playlist
        {
            get { return _Playlist; }
            set { SetProperty(ref _Playlist, value); }
        }

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

            RefreshItems();

            await base.OnNavigatedToAsync(parameters);
        }

        public override void OnNavigatedFrom(INavigationParameters parameters)
        {
            if (Playlist != null)
            {
                WeakReferenceMessenger.Default.Unregister<LocalPlaylistItemRemovedMessage, string>(this, Playlist.PlaylistId.Id);
            }
            base.OnNavigatedFrom(parameters);
        }


        protected override (int, IIncrementalSource<VideoListItemControlViewModel>) GenerateIncrementalSource()
        {
            return (LocalPlaylistIncrementalLoadingSource.OneTimeLoadingCount, new LocalPlaylistIncrementalLoadingSource(Playlist, _nicoVideoProvider));
        }


        void RefreshItems()
        {
            if (Playlist != null)
            {
                WeakReferenceMessenger.Default.Register<LocalPlaylistItemRemovedMessage, string>(this, Playlist.PlaylistId.Id, (r, m) => 
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

        private DelegateCommand<IVideoContent> _PlayWithCurrentPlaylistCommand;
        public DelegateCommand<IVideoContent> PlayWithCurrentPlaylistCommand
        {
            get
            {
                return _PlayWithCurrentPlaylistCommand
                    ?? (_PlayWithCurrentPlaylistCommand = new DelegateCommand<IVideoContent>((video) =>
                    {
                        _messenger.Send(new VideoPlayRequestMessage() { VideoId = video.VideoId, PlaylistId = Playlist.PlaylistId.Id, PlaylistOrigin = Playlist.PlaylistId.Origin });
                    }
                    ));
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
            var targetItems = await _playlist.GetRangeAsync(head, pageSize, ct);
            var items = await _nicoVideoProvider.GetCachedVideoInfoItemsAsync(targetItems.Select(x => x.ItemId));

            ct.ThrowIfCancellationRequested();
            return items.Select(item => new VideoListItemControlViewModel(item));
        }
    }
}
