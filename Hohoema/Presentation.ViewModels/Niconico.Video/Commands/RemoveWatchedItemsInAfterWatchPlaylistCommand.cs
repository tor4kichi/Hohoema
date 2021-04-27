using Hohoema.Models.UseCase.NicoVideos;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Presentation.ViewModels.Niconico.Video.Commands
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
            var currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
            Microsoft.AppCenter.Analytics.Analytics.TrackEvent($"{currentMethod.DeclaringType.Name}#{currentMethod.Name}");

            var removed = _hohoemaPlaylist.RemoveQueueIfWatched();

            System.Diagnostics.Debug.WriteLine("あとで見るから視聴済みを削除： " + removed);
        }
    }
}
