using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Hohoema.Services.Niconico.Player.Events
{
    public sealed class ChangePlayerDisplayViewRequestMessage : ValueChangedMessage<long>
    {
        public ChangePlayerDisplayViewRequestMessage() : base(0)
        {
        }
    }
}
