#nullable enable
using CommunityToolkit.Mvvm.Messaging.Messages;
using Hohoema.Models.VideoCache;

namespace Hohoema.Services.VideoCache.Events;

public sealed class VideoCacheProgressChangedMessage : ValueChangedMessage<VideoCacheItem>
{
    public VideoCacheProgressChangedMessage(VideoCacheItem value) : base(value)
    {
    }
}
