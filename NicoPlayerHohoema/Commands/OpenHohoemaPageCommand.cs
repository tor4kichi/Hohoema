using NicoPlayerHohoema.Models;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Practices.Unity;

namespace NicoPlayerHohoema.Commands
{
    public sealed class OpenHohoemaPageCommand : DelegateCommandBase
    {
        protected override bool CanExecute(object parameter)
        {
            return parameter is ViewModels.MenuItemViewModel;
        }

        protected override void Execute(object parameter)
        {
            if (parameter is ViewModels.MenuItemViewModel)
            {
                var menuItem = parameter as ViewModels.MenuItemViewModel;
                var pageManager = App.Current.Container.Resolve<PageManager>();
                pageManager.OpenPage(menuItem.PageType, menuItem.Parameter);
            }
        }
    }
}
