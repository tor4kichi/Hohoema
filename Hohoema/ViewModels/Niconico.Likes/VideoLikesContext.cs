#nullable enable
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Hohoema.Models.Niconico.Video;
using Hohoema.Models.Notification;
using Hohoema.Models.Player.Video;
using Hohoema.Services;
using I18NPortable;
using NiconicoToolkit.Likes;
using Reactive.Bindings;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Hohoema.ViewModels.Niconico.Likes;

public class LikesChangedMessageData
{
    public string VideoId { get; init; }
    public bool IsLikes { get; init; }
}
public class LikesChangedMessage : ValueChangedMessage<LikesChangedMessageData>
{
    public LikesChangedMessage(LikesChangedMessageData value) : base(value)
    {
    }
}

public sealed class VideoLikesContext : ObservableObject, IRecipient<LikesChangedMessage>
{
    public static readonly VideoLikesContext Default = new VideoLikesContext();

    private VideoLikesContext() { }

    public VideoLikesContext(INicoVideoDetails video, LikesClient likesClient, NotificationService notificationService)
    {
        _video = video;
        _likesClient = likesClient;
        _notificationService = notificationService;
        _isLikes = video.IsLikedVideo;

        WeakReferenceMessenger.Default.Register(this, _video.VideoId);
    }

    private readonly IVideoDetail _video;
    private readonly LikesClient _likesClient;
    private readonly NotificationService _notificationService;

    private bool _isLikes;
    public bool IsLikes
    {
        get { return _isLikes; }
        set 
        {
            if (SetProperty(ref _isLikes, value))
            {
                _ = ProcessLikeAsync(_isLikes);
            }
        }
    }

    private bool _nowLikeProcessing;
    public bool NowLikeProcessing
    {
        get { return _nowLikeProcessing; }
        set { SetProperty(ref _nowLikeProcessing, value); }
    }

    private async Task ProcessLikeAsync(bool like)
    {
        if (NowLikeProcessing)
        {
            return;
        }

        //Microsoft.AppCenter.Analytics.Analytics.TrackEvent("VideoLikesContext#ProcessLikeAsync");
        
        NowLikeProcessing = true;

        try
        {
            if (like)
            {
                var res = await _likesClient.DoLikeVideoAsync(_video.VideoId);
                if (!res.IsSuccess)
                {
                    IsLikes = false;
                }
                else
                {
                    var thanksMessage = res.ThanksMessage;

                    if (!string.IsNullOrEmpty(thanksMessage))
                    {
                        _notificationService.ShowInAppNotification(new InAppNotificationPayload()
                        {
                            Title = "LikeThanksMessageDescWithVideoOwnerName".Translate(_video.ProviderName),
                            Icon = _video.ProviderIconUrl,
                            Content = thanksMessage,
                            IsShowDismissButton = true,
                            ShowDuration = TimeSpan.FromSeconds(7),
                        });
                    }
                }
            }
            else
            {
                var res = await _likesClient.UnDoLikeVideoAsync(_video.VideoId);
                if (!res.IsSuccess)
                {
                    IsLikes = true;
                }
            }
        }
        finally
        {
            await Task.Delay(250);
            NowLikeProcessing = false;
        }

        WeakReferenceMessenger.Default.Send(new LikesChangedMessage(new() { VideoId = _video.VideoId, IsLikes = _isLikes }), _video.VideoId);
    }

    public void Receive(LikesChangedMessage message)
    {
        NowLikeProcessing = true;
        IsLikes = message.Value.IsLikes;
        NowLikeProcessing = false;

        Debug.WriteLine($"Likes changed, videoId:{message.Value.VideoId}, isLikes:{message.Value.IsLikes}");
    }
}
