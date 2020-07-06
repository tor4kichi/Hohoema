using Hohoema.Interfaces;
using Hohoema.Models.Repository;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.UseCase.Playlist.Commands
{
    public sealed class LocalPlaylistAddItemCommand : VideoContentSelectionCommandBase
    {
        private readonly LocalPlaylist _playlist;

        public LocalPlaylistAddItemCommand(LocalPlaylist playlist)
        {
            _playlist = playlist;
        }

        protected override void Execute(IVideoContent content)
        {
            _playlist.AddPlaylistItem(content);
        }

        protected override void Execute(IEnumerable<IVideoContent> items)
        {
            _playlist.AddPlaylistItem(items);
        }
    }
}
