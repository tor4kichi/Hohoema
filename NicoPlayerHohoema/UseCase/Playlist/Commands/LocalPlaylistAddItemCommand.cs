using NicoPlayerHohoema.Interfaces;
using NicoPlayerHohoema.Repository.Playlist;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.UseCase.Playlist.Commands
{
    public sealed class LocalPlaylistAddItemCommand : VideoContentSelectionCommandBase
    {
        private readonly LocalPlaylist _playlist;
        private readonly LocalMylistManager _localMylistManager;

        public LocalPlaylistAddItemCommand(LocalPlaylist playlist, LocalMylistManager localMylistManager)
        {
            _playlist = playlist;
            _localMylistManager = localMylistManager;
        }

        protected override void Execute(IVideoContent content)
        {
            _localMylistManager.AddPlaylistItem(_playlist, content);
        }

        protected override void Execute(IEnumerable<IVideoContent> items)
        {
            _localMylistManager.AddPlaylistItem(_playlist, items);
        }
    }
}
