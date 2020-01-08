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

        public LocalPlaylistDeleteCommand(
            LocalMylistManager localMylistManager
            )
        {
            _localMylistManager = localMylistManager;
        }
        protected override bool CanExecute(object parameter)
        {
            return parameter is LocalPlaylist;
        }

        protected override void Execute(object parameter)
        {
            if (parameter is LocalPlaylist localPlaylist)
            {
                _localMylistManager.RemovePlaylist(localPlaylist);
            }
        }
    }
}
