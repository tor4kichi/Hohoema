
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.UseCase.NicoVideos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Presentation.ViewModels.NicoVideos.Commands
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
            var currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
            Microsoft.AppCenter.Analytics.Analytics.TrackEvent($"{currentMethod.DeclaringType.Name}#{currentMethod.Name}");

            _hohoemaPlaylist.AddWatchAfterPlaylist(content);
        }
    }
}
