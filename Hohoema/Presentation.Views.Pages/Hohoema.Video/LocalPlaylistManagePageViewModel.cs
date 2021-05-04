using Hohoema.Models.Domain.PageNavigation;
using Hohoema.Models.Domain.Playlist;
using Hohoema.Models.UseCase;
using Hohoema.Models.UseCase.NicoVideos;
using Hohoema.Models.UseCase.PageNavigation;
using Hohoema.Presentation.ViewModels.Niconico.Video.Commands;
using Microsoft.Toolkit.Uwp.UI;
using Prism.Commands;
using Prism.Navigation;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Presentation.ViewModels.Pages.Hohoema.Video
{
    public sealed class LocalPlaylistManagePageViewModel : HohoemaPageViewModelBase
    {
        private readonly PageManager _pageManager;
        private readonly LocalMylistManager _localMylistManager;

        public AdvancedCollectionView ItemsView { get; }
        public ApplicationLayoutManager ApplicationLayoutManager { get; }
        public PlaylistPlayAllCommand PlaylistPlayAllCommand { get; }
        public LocalPlaylistCreateCommand CreateLocalMylistCommand { get; }
        public LocalPlaylistDeleteCommand DeleteLocalPlaylistCommand { get; }
        public ReactiveCommand<IPlaylist> OpenMylistCommand { get; }
        public DelegateCommand<LocalPlaylist> RenameLocalPlaylistCommand { get; }

        public LocalPlaylistManagePageViewModel(
            PageManager pageManager,
            Services.DialogService dialogService,
            ApplicationLayoutManager applicationLayoutManager,
            LocalMylistManager localMylistManager,
            PlaylistPlayAllCommand playlistPlayAllCommand,
            LocalPlaylistCreateCommand localPlaylistCreateCommand,
            LocalPlaylistDeleteCommand localPlaylistDeleteCommand
            )
        {
            _pageManager = pageManager;
            ApplicationLayoutManager = applicationLayoutManager;
            _localMylistManager = localMylistManager;
            PlaylistPlayAllCommand = playlistPlayAllCommand;
            CreateLocalMylistCommand = localPlaylistCreateCommand;
            DeleteLocalPlaylistCommand = localPlaylistDeleteCommand;
            ItemsView = new AdvancedCollectionView(_localMylistManager.LocalPlaylists);

            OpenMylistCommand = new ReactiveCommand<IPlaylist>()
                .AddTo(_CompositeDisposable);

            OpenMylistCommand.Subscribe(listItem =>
            {
                _pageManager.OpenPageWithId(HohoemaPageType.LocalPlaylist, listItem.Id);
            });


            RenameLocalPlaylistCommand = new DelegateCommand<LocalPlaylist>(async playlist => 
            {
                var result = await dialogService.GetTextAsync(
                    "RenameLocalPlaylist",
                    "RenameLocalPlaylist_Placeholder",
                    playlist.Label,
                    name => !string.IsNullOrWhiteSpace(name)
                    );

                if (result is not null)
                {
                    playlist.UpdateLabel(result);
                }
            });
        }

        public override void OnNavigatedTo(INavigationParameters parameters)
        {
            base.OnNavigatedTo(parameters);
        }
    }
}
