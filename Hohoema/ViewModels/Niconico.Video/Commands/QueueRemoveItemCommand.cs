
using Hohoema.Models.Niconico.Video;
using Hohoema.Models.Playlist;
using Hohoema.Services.Playlist;
using Hohoema.Services;
using I18NPortable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.ViewModels.Niconico.Video.Commands
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
            foreach (var content in items)
            {
                _queuePlaylist.Remove(content);
            }

            _notificationService.ShowLiteInAppNotification_Success("InAppNotification_MylistRemovedItems_Success".Translate("HohoemaPageType.VideoQueue".Translate(), items.Count()));
        }
    }
}
