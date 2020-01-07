using NicoPlayerHohoema.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.UseCase.Playlist.Commands
{
    public sealed class QueueAddItemCommand : VideoContentSelectionCommandBase
    {
        private readonly HohoemaPlaylist _hohoemaPlaylist;

        public QueueAddItemCommand(HohoemaPlaylist hohoemaPlaylist)
        {
            _hohoemaPlaylist = hohoemaPlaylist;
        }

        protected override void Execute(IVideoContent content)
        {
            _hohoemaPlaylist.AddQueue(content);
        }
    }
}
