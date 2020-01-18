using I18NPortable;
using NicoPlayerHohoema.Services;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.UseCase.Playlist.Commands
{
    public sealed class LocalPlaylistDeleteCommand : DelegateCommandBase
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
                    "DeleteLocalPlaylistDescription".Translate(localPlaylist.Label),
                    "DeleteLocalPlaylistTitle".Translate(),
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
