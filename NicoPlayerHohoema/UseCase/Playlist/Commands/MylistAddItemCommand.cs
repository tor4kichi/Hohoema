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
    class MylistAddItemCommand : VideoContentSelectionCommandBase
    {
        private readonly LoginUserMylistPlaylist _playlist;
        private readonly UserMylistManager _userMylistManager;

        public MylistAddItemCommand(LoginUserMylistPlaylist playlist, UserMylistManager userMylistManager)
        {
            _playlist = playlist;
            _userMylistManager = userMylistManager;
        }

        protected override void Execute(IVideoContent content)
        {
            _userMylistManager.AddItem(_playlist.Id, content.Id);
        }
    }
}
