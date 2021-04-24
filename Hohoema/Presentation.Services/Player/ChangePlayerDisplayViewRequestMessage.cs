using Microsoft.Toolkit.Mvvm.Messaging.Messages;

namespace Hohoema.Presentation.Services.Player
{
    public sealed class ChangePlayerDisplayViewRequestMessage : ValueChangedMessage<long>
    {
        public ChangePlayerDisplayViewRequestMessage() : base(0)
        {
        }
    }
}
