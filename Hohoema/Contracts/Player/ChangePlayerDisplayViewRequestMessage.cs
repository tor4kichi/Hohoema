#nullable enable
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Hohoema.Contracts.Player;

public sealed class ChangePlayerDisplayViewRequestMessage : ValueChangedMessage<long>
{
    public ChangePlayerDisplayViewRequestMessage() : base(0)
    {
    }
}
