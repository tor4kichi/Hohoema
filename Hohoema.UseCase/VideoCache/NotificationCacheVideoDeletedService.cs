using Hohoema.Models.Repository.Niconico.NicoVideo;
using Hohoema.UseCase.VideoCache;
using I18NPortable;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Services.Notification
{
    

    public sealed class NotificationCacheVideoDeletedService
    {
        /// <summary>
        /// Models.Provider.NicoVideoProvider内で検出した動画削除のイベントを受けて
        /// キャッシュされた動画を削除します
        /// </summary>
        /// <param name="eventAggregator"></param>
        /// <param name="videoCacheManager"></param>
        /// <param name="toastNotificationService"></param>
        public NotificationCacheVideoDeletedService(
            NicoVideoProvider nicoVideoProvider,
            VideoCacheManager videoCacheManager,
            IToastNotificationService toastNotificationService
            )
        {
            _nicoVideoProvider = nicoVideoProvider;
            _videoCacheManager = videoCacheManager;
            _toastNotificationService = toastNotificationService;

            _nicoVideoProvider.VideoDeletedFromServer += _nicoVideoProvider_VideoDeletedFromServer;
        }

        private async void _nicoVideoProvider_VideoDeletedFromServer(object sender, VideoDeletedEvent e)
        {
            var videoInfo = e.Deleted;
            if (await _videoCacheManager.DeleteFromNiconicoServer(videoInfo.RawVideoId))
            {
                _toastNotificationService.ShowToast("ToastNotification_VideoDeletedWithId".Translate(videoInfo.RawVideoId)
                    , "ToastNotification_ExplainVideoForceDeletion".Translate(videoInfo?.Title ?? videoInfo.RawVideoId)
                    , Microsoft.Toolkit.Uwp.Notifications.ToastDuration.Long
                    );
            }

        }

        private readonly NicoVideoProvider _nicoVideoProvider;
        private readonly VideoCacheManager _videoCacheManager;
        private readonly IToastNotificationService _toastNotificationService;
    }
}
