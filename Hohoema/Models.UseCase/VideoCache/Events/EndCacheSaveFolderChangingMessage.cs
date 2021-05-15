using Microsoft.Toolkit.Mvvm.Messaging.Messages;

namespace Hohoema.Models.UseCase.VideoCache.Events
{
    public sealed class EndCacheSaveFolderChangingMessage : ValueChangedMessage<long>
    {
        public EndCacheSaveFolderChangingMessage() : base(0)
        {
        }
    }
}
