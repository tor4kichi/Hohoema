﻿#nullable enable

using Hohoema.Models.LocalMylist;
using Hohoema.Models.Niconico.Video;
using Hohoema.Services;
using Hohoema.Services.LocalMylist;
using I18NPortable;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hohoema.ViewModels.Niconico.Video.Commands;

public sealed class LocalPlaylistAddItemCommand : VideoContentSelectionCommandBase
{
    public LocalPlaylist Playlist { get; set; }

    public LocalPlaylistAddItemCommand()
    {
    }

    protected override void Execute(IVideoContent content)
    {
        Execute(new[] { content });
    }

    protected override async void Execute(IEnumerable<IVideoContent> items)
    {
        var playlist = Playlist;
        if (playlist == null)
        {
            var localPlaylistManager = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<LocalMylistManager>();
            var dialogService = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<DialogService>();
            playlist = localPlaylistManager.LocalPlaylists.Any() ?
                await dialogService.ShowSingleSelectDialogAsync(
                localPlaylistManager.LocalPlaylists.ToList(),
                null,
                nameof(LocalPlaylist.Name),                
                "SelectLocalMylist".Translate(),
                "Select".Translate(),
                "CreateNew".Translate(),
                () => CreateLocalPlaylist(),
                (mylist, s) => mylist.Name.Contains(s)
                )
                : await CreateLocalPlaylist()
                ;
        }

        if (playlist != null)
        {
            playlist.AddPlaylistItem(items);
        }
    }

    async Task<LocalPlaylist> CreateLocalPlaylist()
    {
        var localPlaylistManager = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<LocalMylistManager>();
        var dialogService = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<DialogService>();
        var name = await dialogService.GetTextAsync("LocalPlaylistCreate".Translate(), "LocalPlaylistNameTextBoxPlacefolder".Translate(), "", (s) => !string.IsNullOrWhiteSpace(s));
        if (name != null)
        {
            return localPlaylistManager.CreatePlaylist(name);
        }
        else
        {
            return null;
        }
    }
}
