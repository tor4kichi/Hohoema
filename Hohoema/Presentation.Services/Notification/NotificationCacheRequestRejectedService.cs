﻿using I18NPortable;
using Hohoema.Models.Domain.Player.Video.Cache;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Presentation.Services.Notification
{
    public sealed class NotificationCacheRequestRejectedService
    {
        public NotificationCacheRequestRejectedService(
            NotificationService notificationService,
            VideoCacheManagerLegacy videoCacheManager
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
        public VideoCacheManagerLegacy VideoCacheManager { get; }
    }
}
