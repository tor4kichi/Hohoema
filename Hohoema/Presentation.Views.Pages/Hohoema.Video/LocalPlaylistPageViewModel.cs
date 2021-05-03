using Hohoema.Models.UseCase.PageNavigation;
using Hohoema.Models.UseCase;
using Hohoema.Models.UseCase.NicoVideos;
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

namespace Hohoema.Presentation.ViewModels.Pages.Hohoema.Video
{
    public sealed class LocalPlaylistPageViewModel : HohoemaListingPageViewModelBase<VideoInfoControlViewModel>, INavigatedAwareAsync, IPinablePage, ITitleUpdatablePage
    {
        HohoemaPin IPinablePage.GetPin()
        {
            return new HohoemaPin()
            {
                Label = Playlist.Label,
                PageType = HohoemaPageType.LocalPlaylist,
                Parameter = $"id={Playlist.Id}"
            };
        }

        IObservable<string> ITitleUpdatablePage.GetTitleObservable()
        {
            return this.ObserveProperty(x => x.Playlist).Select(x => x?.Label);
        }

        private readonly PageManager _pageManager;
        private readonly LocalMylistManager _localMylistManager;

        public LocalPlaylistPageViewModel(
            ApplicationLayoutManager applicationLayoutManager,
            PageManager pageManager,
            LocalMylistManager localMylistManager,
            HohoemaPlaylist hohoemaPlaylist,
            LocalPlaylistDeleteCommand localPlaylistDeleteCommand,
            PlaylistPlayAllCommand playlistPlayAllCommand,
            SelectionModeToggleCommand selectionModeToggleCommand
            )
        {
            ApplicationLayoutManager = applicationLayoutManager;
            _pageManager = pageManager;
            _localMylistManager = localMylistManager;
            HohoemaPlaylist = hohoemaPlaylist;
            LocalPlaylistDeleteCommand = localPlaylistDeleteCommand;
            PlaylistPlayAllCommand = playlistPlayAllCommand;
            SelectionModeToggleCommand = selectionModeToggleCommand;
        }

        public ApplicationLayoutManager ApplicationLayoutManager { get; }

        public HohoemaPlaylist HohoemaPlaylist { get; }
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
                WeakReferenceMessenger.Default.Unregister<LocalPlaylistItemRemovedMessage>(this);
            }
            base.OnNavigatedFrom(parameters);
        }


        protected override IIncrementalSource<VideoInfoControlViewModel> GenerateIncrementalSource()
        {
            return new LocalPlaylistIncrementalLoadingSource(Playlist);
        }


        void RefreshItems()
        {
            if (Playlist != null)
            {
                WeakReferenceMessenger.Default.Register<LocalPlaylistItemRemovedMessage>(this, (r, m) => 
                {
                    var args = m.Value;
                    foreach (var itemId in args.RemovedItems)
                    {
                        var removedItem = ItemsView.Cast<VideoInfoControlViewModel>().FirstOrDefault(x => x.Id == itemId);
                        if (removedItem != null)
                        {
                            ItemsView.Remove(removedItem);
                        }
                    }
                });

                _localMylistManager.LocalPlaylists.ObserveRemoveChanged()
                    .Subscribe(removed =>
                    {
                        if (Playlist.Id == removed.Id)
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
                        HohoemaPlaylist.PlayContinueWithPlaylist(video, Playlist);
                    }
                    ));
            }
        }
    }

    public class LocalPlaylistIncrementalLoadingSource : HohoemaIncrementalSourceBase<VideoInfoControlViewModel>
    {
        private readonly LocalPlaylist _playlist;

        public List<NicoVideo> _Items { get; private set; }

        public LocalPlaylistIncrementalLoadingSource(LocalPlaylist playlist)
        {
            _playlist = playlist;
        }

        protected override ValueTask<int> ResetSourceImpl()
        {
            _Items = _playlist.GetPlaylistItems();
            return new ValueTask<int>(_Items.Count);
        }

        protected override async IAsyncEnumerable<VideoInfoControlViewModel> GetPagedItemsImpl(int head, int count, [EnumeratorCancellation] CancellationToken ct)
        {
            foreach (var item in _Items.Skip(head).Take(count))
            {
                var vm = new VideoInfoControlViewModel(item);
                await vm.InitializeAsync(ct).ConfigureAwait(false);
                yield return vm;
            }
        }

    }
}
