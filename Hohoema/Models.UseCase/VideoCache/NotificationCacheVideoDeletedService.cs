using I18NPortable;
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Domain.Player.Video.Cache;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Toolkit.Mvvm.Messaging;
using Hohoema.Presentation.Services;

namespace Hohoema.Models.UseCase.VideoCache
{
    public sealed class NotificationCacheVideoDeletedService : IRecipient<VideoDeletedEvent>
    {
        /// <summary>
        /// Models.Provider.NicoVideoProvider内で検出した動画削除のイベントを受けて
        /// キャッシュされた動画を削除します
        /// </summary>
        /// <param name="videoCacheManager"></param>
        /// <param name="notificationService"></param>
        public NotificationCacheVideoDeletedService(
            VideoCacheManagerLegacy videoCacheManager,
            NotificationService notificationService
            )
        {
            VideoCacheManager = videoCacheManager;
            NotificationService = notificationService;

            StrongReferenceMessenger.Default.Register<VideoDeletedEvent>(this);
        }

        public VideoCacheManagerLegacy VideoCacheManager { get; }
        public NotificationService NotificationService { get; }

        public async void Receive(VideoDeletedEvent message)
        {
            var videoInfo = message.Value;
            if (await VideoCacheManager.DeleteFromNiconicoServer(videoInfo.RawVideoId))
            {
                NotificationService.ShowToast("ToastNotification_VideoDeletedWithId".Translate(videoInfo.RawVideoId)
                    , "ToastNotification_ExplainVideoForceDeletion".Translate(videoInfo?.Title ?? videoInfo.RawVideoId)
                    , Microsoft.Toolkit.Uwp.Notifications.ToastDuration.Long
                    );
            }
        }
    }
}
