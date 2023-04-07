
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Domain.Playlist;
using Hohoema.Models.UseCase.Playlist;
using Hohoema.Services;
using Hohoema.ViewModels.VideoListPage;
using I18NPortable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.ViewModels.Niconico.Video.Commands
{
    public sealed class QueueAddItemCommand : VideoContentSelectionCommandBase
    {
        private readonly QueuePlaylist _queuePlaylist;
        private readonly NotificationService _notificationService;

        public QueueAddItemCommand(
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
                if (content is ISourcePlaylistPresenter playlistPresenter)
                {
                    _queuePlaylist.Add(content, playlistPresenter.GetPlaylistId());
                }                
            }

            _notificationService.ShowLiteInAppNotification_Success("InAppNotification_MylistAddedItems_Success".Translate("HohoemaPageType.VideoQueue".Translate(), items.Count()));
        }
    }
}
