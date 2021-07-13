using Microsoft.Toolkit.Mvvm.Messaging.Messages;

namespace Hohoema.Models.UseCase.Niconico.Player.Events
{
    public sealed class ChangePlayerDisplayViewRequestMessage : ValueChangedMessage<long>
    {
        public ChangePlayerDisplayViewRequestMessage() : base(0)
        {
        }
    }
}
