
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.UseCase.NicoVideos;
using Hohoema.Presentation.Services;
using I18NPortable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Presentation.ViewModels.NicoVideos.Commands
{
    public class QueueRemoveItemCommand : VideoContentSelectionCommandBase
    {
        private readonly HohoemaPlaylist _hohoemaPlaylist;
        private readonly NotificationService _notificationService;

        public QueueRemoveItemCommand(
            HohoemaPlaylist hohoemaPlaylist,
            NotificationService notificationService
            )
        {
            _hohoemaPlaylist = hohoemaPlaylist;
            _notificationService = notificationService;
        }

        protected override void Execute(IVideoContent content)
        {
            Execute(new[] { content });
        }

        protected override void Execute(IEnumerable<IVideoContent> items)
        {
            var currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
            Microsoft.AppCenter.Analytics.Analytics.TrackEvent($"{currentMethod.DeclaringType.Name}#{currentMethod.Name}");

            foreach (var content in items)
            {
                _hohoemaPlaylist.RemoveQueue(content);
            }

            _notificationService.ShowLiteInAppNotification_Success("InAppNotification_MylistRemovedItems_Success".Translate("HohoemaPageType.VideoQueue".Translate(), items.Count()));
        }
    }
}
