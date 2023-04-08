#nullable enable
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Hohoema.Services.Player.Events;

public sealed class ChangePlayerDisplayViewRequestMessage : ValueChangedMessage<long>
{
    public ChangePlayerDisplayViewRequestMessage() : base(0)
    {
    }
}
