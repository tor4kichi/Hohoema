using I18NPortable;
using Hohoema.Models.Domain.Player.Video.Cache;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hohoema.Presentation.Services;

namespace Hohoema.Models.UseCase.VideoCache
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
            NotificationService.ShowLiteInAppNotification_Fail("InAppNotification_CanNotCacheChannelVideos_ForContentProtection".Translate());
        }

        public NotificationService NotificationService { get; }
        public VideoCacheManagerLegacy VideoCacheManager { get; }
    }
}
