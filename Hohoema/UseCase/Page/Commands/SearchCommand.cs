using Hohoema.Models;
using Hohoema.Services;
using Hohoema.Services.Page;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.UseCase.Page.Commands
{
    public sealed class SearchCommand : DelegateCommandBase
    {
        private readonly PageManager _pageManager;

        public SearchCommand(PageManager pageManager)
        {
            _pageManager = pageManager;
        }

        protected override bool CanExecute(object parameter)
        {
            return parameter is string;
        }

        protected override void Execute(object parameter)
        {
            if (parameter is string text)
            {
                var searched = Database.SearchHistoryDb.LastSearchedTarget(text);
                SearchTarget searchType = searched ?? SearchTarget.Keyword;

                _pageManager.Search(searchType, text);
            }
        }
    }
}
