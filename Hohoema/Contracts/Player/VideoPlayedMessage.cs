#nullable enable
using CommunityToolkit.Mvvm.Messaging.Messages;
using NiconicoToolkit.Video;
using System;
using static Hohoema.Contracts.Player.VideoWatchedMessage;

namespace Hohoema.Contracts.Player;

public sealed class VideoWatchedMessage : ValueChangedMessage<VideoPlayedEventArgs>
{
    public VideoWatchedMessage(VideoPlayedEventArgs value) : base(value)
    {
    }

    public VideoWatchedMessage(VideoId contentId, TimeSpan playedPosition) 
        : base(new VideoPlayedEventArgs(contentId, playedPosition))
    {
    }

    public VideoWatchedMessage(VideoId contentId)
        : base(new VideoPlayedEventArgs(contentId, TimeSpan.Zero))
    {
    }

    public sealed class VideoPlayedEventArgs
    {
        public VideoPlayedEventArgs(VideoId contentId, TimeSpan playedPosition)
        {
            ContentId = contentId;
            PlayedPosition = playedPosition;
        }

        public VideoId ContentId { get; init; }
        public TimeSpan PlayedPosition { get; init; }
    }
}
