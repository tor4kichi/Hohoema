using I18NPortable;
using Hohoema.Models.Domain.Player.Video.Cache;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hohoema.Presentation.Services;
using Hohoema.Models.Domain.VideoCache;

namespace Hohoema.Models.UseCase.VideoCache
{
    public sealed class NotificationCacheRequestRejectedService
    {
        public NotificationCacheRequestRejectedService(
            NotificationService notificationService,
            VideoCacheManager videoCacheManager
            )
        {
            NotificationService = notificationService;
            VideoCacheManager = videoCacheManager;

            VideoCacheManager.Failed += VideoCacheManager_Failed;
        }

        private void VideoCacheManager_Failed(object sender, VideoCacheFailedEventArgs e)
        {
            // TODO: キャッシュ失敗の通知はトースト通知にしたい
            NotificationService.ShowLiteInAppNotification_Fail(e.VideoCacheDownloadOperationCreationFailedReason.Translate());
        }

        public NotificationService NotificationService { get; }
        public VideoCacheManager VideoCacheManager { get; }
    }
}
