#nullable enable
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Hohoema.Services.VideoCache.Events;

public sealed class EndCacheSaveFolderChangingMessage : ValueChangedMessage<long>
{
    public EndCacheSaveFolderChangingMessage() : base(0)
    {
    }
}
