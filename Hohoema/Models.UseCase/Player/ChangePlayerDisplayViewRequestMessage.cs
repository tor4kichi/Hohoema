using Microsoft.Toolkit.Mvvm.Messaging.Messages;

namespace Hohoema.Models.UseCase.Player
{
    public sealed class ChangePlayerDisplayViewRequestMessage : ValueChangedMessage<long>
    {
        public ChangePlayerDisplayViewRequestMessage() : base(0)
        {
        }
    }
}
