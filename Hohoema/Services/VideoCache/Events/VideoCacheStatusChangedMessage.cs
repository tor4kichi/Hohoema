
using Hohoema.Models.VideoCache;
using CommunityToolkit.Mvvm.Messaging.Messages;
using NiconicoToolkit.Video;

namespace Hohoema.Services.VideoCache.Events
{
    public sealed class VideoCacheStatusChangedMessage : ValueChangedMessage<(VideoId VideoId, VideoCacheStatus? CacheStatus, VideoCacheItem Item)>
    {
        public VideoCacheStatusChangedMessage((VideoId VideoId, VideoCacheStatus?, VideoCacheItem) value) : base(value)
        {
        }
    }
}