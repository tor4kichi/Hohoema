using NicoPlayerHohoema.Services;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.UseCase.Page.Commands
{
    public sealed class OpenPageWithIdCommand : DelegateCommandBase
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
}
