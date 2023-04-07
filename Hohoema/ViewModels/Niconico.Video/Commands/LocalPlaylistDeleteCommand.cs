using I18NPortable;
using Hohoema.Models.Playlist;
using Hohoema.Services;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hohoema.Models.UseCase.Playlist;
using Hohoema.Models.UseCase.Hohoema.LocalMylist;
using Hohoema.Models.LocalMylist;

namespace Hohoema.ViewModels.Niconico.Video.Commands
{
    public sealed class LocalPlaylistDeleteCommand : CommandBase
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
}
