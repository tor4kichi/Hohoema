using Hohoema.Models.Niconico;

namespace Hohoema.ViewModels.Niconico.Account;

public sealed class LogoutFromNiconicoCommand : CommandBase
{
    public LogoutFromNiconicoCommand(
        NiconicoSession niconicoSession
        )
    {
        NiconicoSession = niconicoSession;
    }

    public NiconicoSession NiconicoSession { get; }

    protected override bool CanExecute(object parameter)
    {
        return true;
    }

    protected override async void Execute(object parameter)
    {
        await NiconicoSession.SignOut();
    }
}
