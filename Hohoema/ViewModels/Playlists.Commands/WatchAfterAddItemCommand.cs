using Hohoema.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.UseCase.Playlist.Commands
{
    public sealed class WatchAfterAddItemCommand : VideoContentSelectionCommandBase
    {
        private readonly HohoemaPlaylist _hohoemaPlaylist;

        public WatchAfterAddItemCommand(HohoemaPlaylist hohoemaPlaylist)
        {
            _hohoemaPlaylist = hohoemaPlaylist;
        }

        protected override void Execute(IVideoContent content)
        {
            _hohoemaPlaylist.AddWatchAfterPlaylist(content);
        }
    }
}
