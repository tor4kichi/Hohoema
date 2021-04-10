using I18NPortable;
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Domain.Player.Video.Cache;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Presentation.Services.Notification
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
            VideoCacheManagerLegacy videoCacheManager,
            Services.NotificationService notificationService
            )
        {
            EventAggregator = eventAggregator;
            VideoCacheManager = videoCacheManager;
            NotificationService = notificationService;

            EventAggregator.GetEvent<VideoDeletedEvent>()
                .Subscribe(async videoInfo => 
                {
                    if (await VideoCacheManager.DeleteFromNiconicoServer(videoInfo.RawVideoId))
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
        public VideoCacheManagerLegacy VideoCacheManager { get; }
        public NotificationService NotificationService { get; }
    }
}
