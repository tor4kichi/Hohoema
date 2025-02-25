#nullable enable
using Hohoema.Models.Niconico.Search;

namespace Hohoema.ViewModels.Niconico.Search;

public sealed partial class RemoveSearchHistoryCommand : CommandBase
{
    private readonly SearchHistoryRepository _searchHistoryRepository;

    public RemoveSearchHistoryCommand(SearchHistoryRepository searchHistoryRepository)
    {
        _searchHistoryRepository = searchHistoryRepository;
    }

    protected override bool CanExecute(object parameter)
    {
        return parameter is SearchHistory;
    }

    protected override void Execute(object parameter)
    {
        if (parameter is SearchHistory)
        {
            var history = parameter as SearchHistory;
            _searchHistoryRepository.Remove(history.Keyword, history.Target);
        }
    }
}
