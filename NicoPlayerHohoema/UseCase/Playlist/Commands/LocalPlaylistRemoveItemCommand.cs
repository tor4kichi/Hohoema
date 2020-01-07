using NicoPlayerHohoema.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.UseCase.Playlist.Commands
{
    public sealed class LocalPlaylistRemoveItemCommand : VideoContentSelectionCommandBase
    {
        private readonly LocalPlaylist _playlist;
        private readonly LocalMylistManager _localMylistManager;

        public LocalPlaylistRemoveItemCommand(LocalPlaylist playlist, LocalMylistManager localMylistManager)
        {
            _playlist = playlist;
            _localMylistManager = localMylistManager;
        }

        protected override void Execute(IVideoContent content)
        {
            _localMylistManager.RemovePlaylistItem(_playlist, content);
        }

        protected override void Execute(IEnumerable<IVideoContent> items)
        {
            _localMylistManager.RemovePlaylistItems(_playlist, items);
        }
    }
}
