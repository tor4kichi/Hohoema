using Hohoema.Models.Domain;
using Hohoema.Models.Domain.Niconico.Search;
using Hohoema.Models.Domain.PageNavigation;
using Hohoema.Presentation.Services;
using Hohoema.Presentation.Services.Page;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Presentation.ViewModels.Navigation.Commands
{
    public sealed class SearchCommand : DelegateCommandBase
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
            var currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
            Microsoft.AppCenter.Analytics.Analytics.TrackEvent($"{currentMethod.DeclaringType.Name}#{currentMethod.Name}");

            if (parameter is string text)
            {
                var searched = _searchHistoryRepository.LastSearchedTarget(text);
                SearchTarget searchType = searched ?? SearchTarget.Keyword;

                _pageManager.Search(searchType, text);
            }
        }
    }
}
