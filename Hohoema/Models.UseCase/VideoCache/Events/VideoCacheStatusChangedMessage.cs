
using Hohoema.Models.Domain.VideoCache;
using Microsoft.Toolkit.Mvvm.Messaging.Messages;

namespace Hohoema.Models.UseCase.VideoCache.Events
{
    public sealed class VideoCacheStatusChangedMessage : ValueChangedMessage<(string VideoId, VideoCacheStatus? CacheStatus, VideoCacheItem Item)>
    {
        public VideoCacheStatusChangedMessage((string VideoId, VideoCacheStatus?, VideoCacheItem) value) : base(value)
        {
        }
    }
}