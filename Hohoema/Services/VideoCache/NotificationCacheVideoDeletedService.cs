#nullable enable
using CommunityToolkit.Mvvm.Messaging;
using Hohoema.Models.Niconico.Video;
using Hohoema.Models.VideoCache;
using I18NPortable;

namespace Hohoema.Services.VideoCache;

public sealed class NotificationCacheVideoDeletedService : IRecipient<VideoDeletedMessage>
{
    private readonly IMessenger _messenger;

    /// <summary>
    /// Models.Provider.NicoVideoProvider内で検出した動画削除のイベントを受けて
    /// キャッシュされた動画を削除します
    /// </summary>
    /// <param name="videoCacheManager"></param>
    /// <param name="notificationService"></param>
    public NotificationCacheVideoDeletedService(
        IMessenger messenger,
        VideoCacheManager videoCacheManager,
        NotificationService notificationService
        
        )
    {
        _messenger = messenger;
        VideoCacheManager = videoCacheManager;
        NotificationService = notificationService;

        _messenger.Register<VideoDeletedMessage>(this);
    }

    public VideoCacheManager VideoCacheManager { get; }
    public NotificationService NotificationService { get; }

    public async void Receive(VideoDeletedMessage message)
    {
        var videoInfo = message.Value;
        if (await VideoCacheManager.DeleteFromNiconicoServer(videoInfo.VideoId))
        {
            NotificationService.ShowToast("ToastNotification_VideoDeletedWithId".Translate(videoInfo.VideoId)
                , "ToastNotification_ExplainVideoForceDeletion".Translate(videoInfo?.Title ?? videoInfo.VideoId)
                , Microsoft.Toolkit.Uwp.Notifications.ToastDuration.Long
                );
        }
    }
}
