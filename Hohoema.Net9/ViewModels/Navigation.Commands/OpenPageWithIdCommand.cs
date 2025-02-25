#nullable enable
using CommunityToolkit.Mvvm.Messaging;
using Hohoema.Models.PageNavigation;

namespace Hohoema.ViewModels.Navigation.Commands;

public sealed partial class OpenPageWithIdCommand : CommandBase
{
    private readonly HohoemaPageType _hohoemaPage;
    private readonly IMessenger _messenger;

    public OpenPageWithIdCommand(HohoemaPageType hohoemaPage, IMessenger messenger)        
    {
        _hohoemaPage = hohoemaPage;
        _messenger = messenger;
    }
    protected override bool CanExecute(object parameter)
    {
        return parameter is string;
    }

    protected override void Execute(object parameter)
    {
        if (parameter is string id)
        {
            _ = _messenger.OpenPageWithIdAsync(_hohoemaPage, id);
        }
    }
}
