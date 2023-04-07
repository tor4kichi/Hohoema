using Hohoema.Models.PageNavigation;
using Hohoema.Models.Playlist;
using Hohoema.Models.UseCase;
using Hohoema.Models.UseCase.Playlist;
using Hohoema.Models.UseCase.PageNavigation;
using Hohoema.ViewModels.Niconico.Video.Commands;
using Microsoft.Toolkit.Uwp.UI;
using CommunityToolkit.Mvvm.Input;
using Hohoema.Navigations;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hohoema.Models.UseCase.Hohoema.LocalMylist;
using Hohoema.Models.LocalMylist;

namespace Hohoema.ViewModels.Pages.Hohoema.LocalMylist
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
        public RelayCommand<LocalPlaylist> RenameLocalPlaylistCommand { get; }

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
                _pageManager.OpenPageWithId(HohoemaPageType.LocalPlaylist, listItem.PlaylistId.Id);
            });


            RenameLocalPlaylistCommand = new RelayCommand<LocalPlaylist>(async playlist => 
            {
                var result = await dialogService.GetTextAsync(
                    "RenameLocalPlaylist",
                    "RenameLocalPlaylist_Placeholder",
                    playlist.Name,
                    name => !string.IsNullOrWhiteSpace(name)
                    );

                if (result is not null)
                {
                    playlist.Name = result;
                }
            });
        }

        public override void OnNavigatedTo(INavigationParameters parameters)
        {
            base.OnNavigatedTo(parameters);
        }
    }
}
