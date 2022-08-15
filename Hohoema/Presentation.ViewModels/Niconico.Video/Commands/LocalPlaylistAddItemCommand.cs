
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Domain.Playlist;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using I18NPortable;
using Hohoema.Presentation.Services;
using Hohoema.Models.UseCase.Playlist;
using Hohoema.Models.Domain.LocalMylist;
using Hohoema.Models.UseCase.Hohoema.LocalMylist;

namespace Hohoema.Presentation.ViewModels.Niconico.Video.Commands
{
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
                    nameof(LocalPlaylist.Name),
                    (mylist, s) => mylist.Name.Contains(s),
                    "SelectLocalMylist".Translate(),
                    "Select".Translate(),
                    "CreateNew".Translate(),
                    () => CreateLocalPlaylist()
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
}
