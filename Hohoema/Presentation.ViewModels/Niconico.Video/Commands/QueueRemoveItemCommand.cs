
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Domain.Playlist;
using Hohoema.Models.UseCase.Playlist;
using Hohoema.Presentation.Services;
using I18NPortable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Presentation.ViewModels.Niconico.Video.Commands
{
    public class QueueRemoveItemCommand : VideoContentSelectionCommandBase
    {
        private readonly QueuePlaylist _queuePlaylist;
        private readonly NotificationService _notificationService;

        public QueueRemoveItemCommand(
            QueuePlaylist queuePlaylist,
            NotificationService notificationService
            )
        {
            _queuePlaylist = queuePlaylist;
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
                _queuePlaylist.Remove(content.VideoId);
            }

            _notificationService.ShowLiteInAppNotification_Success("InAppNotification_MylistRemovedItems_Success".Translate("HohoemaPageType.VideoQueue".Translate(), items.Count()));
        }
    }
}
