using CommunityToolkit.Mvvm.Input;
using Hohoema.Models.Niconico.Search;
using Hohoema.Models.PageNavigation;

namespace Hohoema.ViewModels.Pages.Niconico.Search
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

        
        private RelayCommand _DeleteSearchHistoryItemCommand;
        public RelayCommand DeleteSearchHistoryItemCommand
        {
            get
            {
                return _DeleteSearchHistoryItemCommand
                    ?? (_DeleteSearchHistoryItemCommand = new RelayCommand(() =>
                    {
                        SearchPageVM.DeleteSearchHistoryItemCommand.Execute(SearchHistory);
                    }
                    ));
            }
        }
    }
}
