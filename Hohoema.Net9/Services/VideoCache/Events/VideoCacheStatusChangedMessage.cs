#nullable enable

using CommunityToolkit.Mvvm.Messaging.Messages;
using Hohoema.Models.VideoCache;
using NiconicoToolkit.Video;

namespace Hohoema.Services.VideoCache.Events;

public sealed class VideoCacheStatusChangedMessage : ValueChangedMessage<(VideoId VideoId, VideoCacheStatus? CacheStatus, VideoCacheItem Item)>
{
    public VideoCacheStatusChangedMessage((VideoId VideoId, VideoCacheStatus?, VideoCacheItem) value) : base(value)
    {
    }
}