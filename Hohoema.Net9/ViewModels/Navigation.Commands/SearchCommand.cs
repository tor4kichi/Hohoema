#nullable enable
using CommunityToolkit.Mvvm.Messaging;
using Hohoema.Models.Niconico.Search;
using Hohoema.Models.PageNavigation;

namespace Hohoema.ViewModels.Navigation.Commands;

public sealed partial class SearchCommand : CommandBase
{
    private readonly IMessenger _messenger;

    public SearchCommand(IMessenger messenger)         
    {
        _messenger = messenger;
    }

    protected override bool CanExecute(object parameter)
    {
        return parameter is string;
    }

    protected override void Execute(object parameter)
    {
        if (parameter is string text)
        {
            _ = _messenger.OpenSearchPageAsync(SearchTarget.Keyword, text);
        }
    }
}
