using Prism.Commands;
using Hohoema.Models.Domain.Niconico.Search;
using Hohoema.Models.Domain.PageNavigation;

namespace Hohoema.Presentation.ViewModels.Pages.Niconico.Search
{
    public class SearchHistoryListItemViewModel : ISearchHistory
    {
        public SearchHistory SearchHistory { get; }
        public string Keyword { get; private set; }
		public SearchTarget Target { get; private set; }

        SearchPageViewModel SearchPageVM { get; }

        public SearchHistoryListItemViewModel(SearchHistory source, SearchPageViewModel parentVM)
		{
            SearchHistory = source;
            SearchPageVM = parentVM;
            Keyword = source.Keyword;
			Target = source.Target;
		}

        
        private DelegateCommand _DeleteSearchHistoryItemCommand;
        public DelegateCommand DeleteSearchHistoryItemCommand
        {
            get
            {
                return _DeleteSearchHistoryItemCommand
                    ?? (_DeleteSearchHistoryItemCommand = new DelegateCommand(() =>
                    {
                        SearchPageVM.DeleteSearchHistoryItemCommand.Execute(SearchHistory);
                    }
                    ));
            }
        }
    }
}
