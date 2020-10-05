using Hohoema.Models.Domain.Niconico.Search;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Presentation.ViewModels.Search
{
    public sealed class RemoveSearchHistoryCommand : DelegateCommandBase
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
}
