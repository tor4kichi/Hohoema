#nullable enable
using CommunityToolkit.Mvvm.Messaging.Messages;
using NiconicoToolkit.Video;
using System;

namespace Hohoema.Services.Player.Events;

public sealed class VideoPlayedMessage : ValueChangedMessage<Events.VideoPlayedMessage.VideoPlayedEventArgs>
{
    public VideoPlayedMessage(VideoPlayedEventArgs value) : base(value)
    {
    }

    public sealed class VideoPlayedEventArgs
    {
        public VideoId ContentId { get; set; }
        public TimeSpan PlayedPosition { get; set; }
    }
}
