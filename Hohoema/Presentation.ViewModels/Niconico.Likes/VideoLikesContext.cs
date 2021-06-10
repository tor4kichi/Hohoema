using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Domain.Notification;
using Hohoema.Models.Domain.Player.Video;
using Hohoema.Presentation.Services;
using I18NPortable;
using Microsoft.Toolkit.Mvvm.Messaging;
using Microsoft.Toolkit.Mvvm.Messaging.Messages;
using NiconicoToolkit.Likes;
using Prism.Commands;
using Prism.Mvvm;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Presentation.ViewModels.Niconico.Likes
{
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

    public sealed class VideoLikesContext : BindableBase, IRecipient<LikesChangedMessage>
    {
        public static readonly VideoLikesContext Default = new VideoLikesContext();

        private VideoLikesContext() { }

        public VideoLikesContext(INicoVideoDetails video, LikesClient likesClient, NotificationService notificationService)
        {
            _video = video;
            _likesClient = likesClient;
            _notificationService = notificationService;
            _isLikes = video.IsLikedVideo;

            WeakReferenceMessenger.Default.Register(this, _video.Id);
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

            Microsoft.AppCenter.Analytics.Analytics.TrackEvent("VideoLikesContext#ProcessLikeAsync");
            
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
                    var res = await _likesClient.UnDoLikeVideoAsync(_video.Id);
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

            WeakReferenceMessenger.Default.Send(new LikesChangedMessage(new() { VideoId = _video.Id, IsLikes = _isLikes }), _video.Id);
        }

        public void Receive(LikesChangedMessage message)
        {
            NowLikeProcessing = true;
            IsLikes = message.Value.IsLikes;
            NowLikeProcessing = false;

            Debug.WriteLine($"Likes changed, videoId:{message.Value.VideoId}, isLikes:{message.Value.IsLikes}");
        }
    }
}
