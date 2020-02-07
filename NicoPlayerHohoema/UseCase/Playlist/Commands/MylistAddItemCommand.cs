using NicoPlayerHohoema.Interfaces;
using NicoPlayerHohoema.Models;
using NicoPlayerHohoema.Repository.Playlist;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.UseCase.Playlist.Commands
{
    public class MylistAddItemCommand : VideoContentSelectionCommandBase
    {
        private readonly LoginUserMylistPlaylist _playlist;

        public MylistAddItemCommand(LoginUserMylistPlaylist playlist)
        {
            _playlist = playlist;
        }

        protected override void Execute(IVideoContent content)
        {
            _playlist.AddItem(content.Id);
        }
    }
}
