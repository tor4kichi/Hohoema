using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.UseCase.Playlist.Commands
{
    public sealed class RemoveWatchedItemsInAfterWatchPlaylistCommand : DelegateCommandBase
    {
        private readonly HohoemaPlaylist _hohoemaPlaylist;

        public RemoveWatchedItemsInAfterWatchPlaylistCommand(HohoemaPlaylist hohoemaPlaylist)
        {
            _hohoemaPlaylist = hohoemaPlaylist;
        }

        protected override bool CanExecute(object parameter)
        {
            return true;
        }

        protected override void Execute(object parameter)
        {
            var removed = _hohoemaPlaylist.RemoveWatchAfterIfWatched();

            System.Diagnostics.Debug.WriteLine("あとで見るから視聴済みを削除： " + removed);
        }
    }
}
