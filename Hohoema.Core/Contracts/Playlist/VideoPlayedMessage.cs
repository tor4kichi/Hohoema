#nullable enable
using CommunityToolkit.Mvvm.Messaging.Messages;
using NiconicoToolkit.Video;
using System;

namespace Hohoema.Contracts.Playlist;

public sealed class VideoWatchedMessage : ValueChangedMessage<VideoPlayedMessageData>
{
    public VideoWatchedMessage(VideoPlayedMessageData value) : base(value)
    {
    }

    public VideoWatchedMessage(VideoId contentId, TimeSpan playedPosition) 
        : base(new VideoPlayedMessageData(contentId, playedPosition))
    {
    }

    public VideoWatchedMessage(VideoId contentId)
        : base(new VideoPlayedMessageData(contentId, TimeSpan.Zero))
    {
    }
}


public readonly struct VideoPlayedMessageData
{
    public VideoPlayedMessageData(VideoId contentId, TimeSpan playedPosition)
    {
        ContentId = contentId;
        PlayedPosition = playedPosition;
    }

    public VideoId ContentId { get; init; }
    public TimeSpan PlayedPosition { get; init; }
}
