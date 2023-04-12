#nullable enable
using Hohoema.Models.Niconico.Video;
using Hohoema.Services;
using Hohoema.Services.LocalMylist;
using I18NPortable;
using System;
using System.Diagnostics;

namespace Hohoema.ViewModels.Niconico.Video.Commands;

public sealed class LocalPlaylistCreateCommand : CommandBase
{
    public LocalPlaylistCreateCommand(
        LocalMylistManager localMylistManager,
        DialogService dialogService
        )
    {
        LocalMylistManager = localMylistManager;
        DialogService = dialogService;
    }

    public LocalMylistManager LocalMylistManager { get; }
    public DialogService DialogService { get; }

    protected override bool CanExecute(object parameter)
    {
        return true;
    }

    protected override async void Execute(object parameter)
    {
        var result = await DialogService.GetTextAsync(
            "LocalPlaylistCreate".Translate(),
            "LocalPlaylistNameTextBoxPlacefolder".Translate(), 
            "", 
            (s) => !string.IsNullOrWhiteSpace(s)
            );
        if (result != null)
        {
            var localPlaylist = LocalMylistManager.CreatePlaylist(result);

            Debug.WriteLine("ローカルマイリスト作成：" + result);

            if (parameter is IVideoContent content)
            {
                localPlaylist.AddPlaylistItem(content);
            }
            else if (parameter is string itemId)
            {
                throw new NotSupportedException();
            }
        }
    }
}
