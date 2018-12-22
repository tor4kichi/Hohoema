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
                    var deletedCount = await VideoCacheManager.DeleteCachedVideo(videoInfo.RawVideoId);

                    if (deletedCount > 0)
                    {
                        NotificationService.ShowToast("動画削除：" + videoInfo.RawVideoId
                            , $"『{videoInfo?.Title ?? videoInfo.RawVideoId}』 はニコニコ動画サーバーから削除されたため、キャッシュを強制削除しました。"
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
