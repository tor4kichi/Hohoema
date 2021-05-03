
using Hohoema.Models.Domain.VideoCache;
using Microsoft.Toolkit.Mvvm.Messaging.Messages;

namespace Hohoema.Models.UseCase.VideoCache.Events
{
    public sealed class VideoCacheStatusChangedMessage : ValueChangedMessage<(VideoCacheStatus? CacheStatus, VideoCacheItem Item)>
    {
        public VideoCacheStatusChangedMessage((VideoCacheStatus?, VideoCacheItem) value) : base(value)
        {
        }
    }
}