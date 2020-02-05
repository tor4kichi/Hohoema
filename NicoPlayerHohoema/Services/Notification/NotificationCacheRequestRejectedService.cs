using I18NPortable;
using NicoPlayerHohoema.Models.Cache;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Services.Notification
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

            VideoCacheManager.Rejected += VideoCacheManager_Rejected;
        }

        private void VideoCacheManager_Rejected(object sender, CacheRequestRejectedEventArgs e)
        {
            NotificationService.ShowInAppNotification(new InAppNotificationPayload()
            {
                Content = "InAppNotification_CanNotCacheChannelVideos_ForContentProtection".Translate(),
            });
        }

        public HohoemaNotificationService HohoemaNotificationService { get; }
        public NotificationService NotificationService { get; }
        public VideoCacheManager VideoCacheManager { get; }
    }
}
