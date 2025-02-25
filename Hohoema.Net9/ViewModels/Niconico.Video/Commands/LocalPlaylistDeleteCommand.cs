#nullable enable
using Hohoema.Models.LocalMylist;
using Hohoema.Services;
using Hohoema.Services.LocalMylist;
using I18NPortable;
using System;

namespace Hohoema.ViewModels.Niconico.Video.Commands;

public sealed partial class LocalPlaylistDeleteCommand : CommandBase
{
    private readonly LocalMylistManager _localMylistManager;
    private readonly DialogService _dialogService;

    public LocalPlaylistDeleteCommand(
        LocalMylistManager localMylistManager,
        DialogService dialogService
        )
    {
        _localMylistManager = localMylistManager;
        _dialogService = dialogService;
    }
    protected override bool CanExecute(object parameter)
    {
        return parameter is LocalPlaylist;
    }

    protected override async void Execute(object parameter)
    {
        if (parameter is LocalPlaylist localPlaylist)
        {
            if (await _dialogService.ShowMessageDialog(
                "DeleteLocalPlaylistDescription".Translate(localPlaylist.Name),
                "DeleteLocalPlaylistTitle".Translate(localPlaylist.Name),
                "Delete".Translate(),
                "Cancel".Translate()
                ))
            {
                _localMylistManager.RemovePlaylist(localPlaylist);

            }
        }
    }
}
