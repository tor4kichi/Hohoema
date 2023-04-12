#nullable enable
using Hohoema.Models.Niconico.Search;
using Hohoema.Models.PageNavigation;

namespace Hohoema.ViewModels.Navigation.Commands;

public sealed class SearchCommand : CommandBase
{
    private readonly PageManager _pageManager;
    private readonly SearchHistoryRepository _searchHistoryRepository;

    public SearchCommand(PageManager pageManager, SearchHistoryRepository searchHistoryRepository)
    {
        _pageManager = pageManager;
        _searchHistoryRepository = searchHistoryRepository;
    }

    protected override bool CanExecute(object parameter)
    {
        return parameter is string;
    }

    protected override void Execute(object parameter)
    {
        if (parameter is string text)
        {
//                var searched = _searchHistoryRepository.LastSearchedTarget(text);
//                SearchTarget searchType = searched ?? SearchTarget.Keyword;

            _pageManager.Search(SearchTarget.Keyword, text);
        }
    }
}
