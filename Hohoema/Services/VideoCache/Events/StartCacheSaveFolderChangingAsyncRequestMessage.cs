using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Hohoema.Services.VideoCache.Events;

public sealed class StartCacheSaveFolderChangingAsyncRequestMessage : AsyncRequestMessage<long>
{
    public StartCacheSaveFolderChangingAsyncRequestMessage() 
    {
    }
}
