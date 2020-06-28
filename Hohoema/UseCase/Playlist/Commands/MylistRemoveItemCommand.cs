using Hohoema.Interfaces;
using Hohoema.Models;
using Hohoema.Repository.Playlist;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.UseCase.Playlist.Commands
{
    public sealed class MylistRemoveItemCommand : VideoContentSelectionCommandBase
    {
        private readonly LoginUserMylistPlaylist _playlist;

        public MylistRemoveItemCommand(LoginUserMylistPlaylist playlist)
        {
            _playlist = playlist;
        }

        protected override void Execute(IVideoContent content)
        {
            _playlist.RemoveItem(content.Id);
        }
    }
}
