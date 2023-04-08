using CommunityToolkit.Mvvm.Messaging;
using Hohoema.Services.Player.Events;

namespace Hohoema.ViewModels.Player;

public sealed class TogglePlayerDisplayViewCommand : CommandBase
{
    private readonly IMessenger _messenger;

    public TogglePlayerDisplayViewCommand(IMessenger messenger)
    {
        _messenger = messenger;
    }
    protected override bool CanExecute(object parameter)
    {
        return true;
    }

    protected override void Execute(object parameter)
    {
        _messenger.Send(new ChangePlayerDisplayViewRequestMessage());
    }
}
