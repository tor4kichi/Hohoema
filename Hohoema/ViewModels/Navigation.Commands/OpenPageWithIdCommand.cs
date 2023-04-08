using Hohoema.Models.PageNavigation;

namespace Hohoema.ViewModels.Navigation.Commands;

public sealed class OpenPageWithIdCommand : CommandBase
{
    private readonly HohoemaPageType _hohoemaPage;
    private readonly PageManager _pageManager;

    public OpenPageWithIdCommand(HohoemaPageType hohoemaPage, PageManager pageManager)
    {
        _hohoemaPage = hohoemaPage;
        _pageManager = pageManager;
    }
    protected override bool CanExecute(object parameter)
    {
        return parameter is string;
    }

    protected override void Execute(object parameter)
    {
        if (parameter is string id)
        {
            _pageManager.OpenPageWithId(_hohoemaPage, id);
        }
    }
}
