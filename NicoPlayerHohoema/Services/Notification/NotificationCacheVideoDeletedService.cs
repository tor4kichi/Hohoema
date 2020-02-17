using I18NPortable;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Services.Notification
{
    

    public sealed class NotificationCacheVideoDeletedService
    {
        /// <summary>
        /// Models.Provider.NicoVideoProvider内で検出した動画削除のイベントを受けて
        /// キャッシュされた動画を削除します
        /// </summary>
        /// <param name="eventAggregator"></param>
        /// <param name="videoCacheManager"></param>
        /// <param name="notificationService"></param>
        public NotificationCacheVideoDeletedService(
            IEventAggregator eventAggregator,
            Models.Cache.VideoCacheManager videoCacheManager,
            Services.NotificationService notificationService
            )
        {
            EventAggregator = eventAggregator;
            VideoCacheManager = videoCacheManager;
            NotificationService = notificationService;

            EventAggregator.GetEvent<Models.Provider.VideoDeletedEvent>()
                .Subscribe(async videoInfo => 
                {
                    if (await VideoCacheManager.CancelCacheRequest(videoInfo.RawVideoId))
                    {
                        NotificationService.ShowToast("ToastNotification_VideoDeletedWithId".Translate(videoInfo.RawVideoId)
                            , "ToastNotification_ExplainVideoForceDeletion".Translate(videoInfo?.Title ?? videoInfo.RawVideoId)
                            , Microsoft.Toolkit.Uwp.Notifications.ToastDuration.Long
                            );
                    }
                }
                , keepSubscriberReferenceAlive: true
                );
        }

        public IEventAggregator EventAggregator { get; }
        public Models.Cache.VideoCacheManager VideoCacheManager { get; }
        public NotificationService NotificationService { get; }
    }
}
