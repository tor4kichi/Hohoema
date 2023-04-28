#nullable enable
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Hohoema.Models.LocalMylist;
using Hohoema.Models.PageNavigation;
using Hohoema.Models.Playlist;
using Hohoema.Services;
using Hohoema.Services.LocalMylist;
using Hohoema.ViewModels.Niconico.Video.Commands;
using Microsoft.Toolkit.Uwp.UI;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;

namespace Hohoema.ViewModels.Pages.Hohoema.LocalMylist;

public sealed class LocalPlaylistManagePageViewModel : HohoemaPageViewModelBase
{
    private readonly IMessenger _messenger;
    private readonly LocalMylistManager _localMylistManager;

    public AdvancedCollectionView ItemsView { get; }
    public ApplicationLayoutManager ApplicationLayoutManager { get; }
    public PlaylistPlayAllCommand PlaylistPlayAllCommand { get; }
    public LocalPlaylistCreateCommand CreateLocalMylistCommand { get; }
    public LocalPlaylistDeleteCommand DeleteLocalPlaylistCommand { get; }
    public ReactiveCommand<IPlaylist> OpenMylistCommand { get; }
    public RelayCommand<LocalPlaylist> RenameLocalPlaylistCommand { get; }

    public LocalPlaylistManagePageViewModel(
        IMessenger messenger,
        Services.DialogService dialogService,
        ApplicationLayoutManager applicationLayoutManager,
        LocalMylistManager localMylistManager,
        PlaylistPlayAllCommand playlistPlayAllCommand,
        LocalPlaylistCreateCommand localPlaylistCreateCommand,
        LocalPlaylistDeleteCommand localPlaylistDeleteCommand
        )
    {
        _messenger = messenger;
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
            _ = _messenger.OpenPageWithIdAsync(HohoemaPageType.LocalPlaylist, listItem.PlaylistId.Id);
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
