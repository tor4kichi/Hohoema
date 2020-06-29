using I18NPortable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hohoema.UseCase.Services;
using Hohoema.UseCase.VideoCache;
using Hohoema.UseCase.Events;

namespace Hohoema.Services.Notification
{
    public sealed class NotificationCacheRequestRejectedService
    {
        public NotificationCacheRequestRejectedService(
            IInAppNotificationService notificationService,
            VideoCacheManager videoCacheManager
            )
        {
            NotificationService = notificationService;
            VideoCacheManager = videoCacheManager;

            VideoCacheManager.Rejected += VideoCacheManager_Rejected;
        }

        private void VideoCacheManager_Rejected(object sender, CacheRequestRejectedEventArgs e)
        {
            NotificationService.ShowInAppNotification(new InAppNotificationPayload()
            {
                Content = "InAppNotification_CanNotCacheChannelVideos_ForContentProtection".Translate(),
            });
        }

        public IInAppNotificationService NotificationService { get; }
        public VideoCacheManager VideoCacheManager { get; }
    }
}
