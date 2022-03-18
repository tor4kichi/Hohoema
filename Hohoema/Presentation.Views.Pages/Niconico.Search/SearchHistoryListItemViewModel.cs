﻿using Microsoft.Toolkit.Mvvm.Input;
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
