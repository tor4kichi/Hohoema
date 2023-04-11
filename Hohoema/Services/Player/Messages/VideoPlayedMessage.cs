#nullable enable
using CommunityToolkit.Mvvm.Messaging.Messages;
using NiconicoToolkit.Video;
using System;

namespace Hohoema.Services.Player.Events;

public sealed class VideoWatchedMessage : ValueChangedMessage<Events.VideoWatchedMessage.VideoPlayedEventArgs>
{
    public VideoWatchedMessage(VideoPlayedEventArgs value) : base(value)
    {
    }

    public VideoWatchedMessage(VideoId contentId, TimeSpan playedPosition) 
        : base(new VideoPlayedEventArgs() { ContentId = contentId, PlayedPosition = playedPosition })
    {
    }

    public VideoWatchedMessage(VideoId contentId)
        : base(new VideoPlayedEventArgs() { ContentId = contentId, PlayedPosition = TimeSpan.Zero })
    {
    }

    public sealed class VideoPlayedEventArgs
    {
        public VideoId ContentId { get; init; }
        public TimeSpan PlayedPosition { get; init; }
    }
}
