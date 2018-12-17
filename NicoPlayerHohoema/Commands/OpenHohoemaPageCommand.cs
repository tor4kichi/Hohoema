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
            if (parameter is string)
            {
                return Enum.TryParse<Models.HohoemaPageType>(parameter as string, out var type);
            }
            else
            {
                return parameter is ViewModels.MenuItemViewModel || parameter is HohoemaPin;
            }
        }

        protected override void Execute(object parameter)
        {
            if (parameter is string)
            {
                if (Enum.TryParse<Models.HohoemaPageType>(parameter as string, out var pageType))
                {
                    var pageManager = HohoemaCommnadHelper.GetPageManager();
                    pageManager.OpenPage(pageType);
                }
            }
            else if (parameter is ViewModels.MenuItemViewModel item)
            {
                var pageManager = HohoemaCommnadHelper.GetPageManager();
                pageManager.OpenPage(item.PageType, item.Parameter);
            }
            else if (parameter is HohoemaPin pin)
            {
                var pageManager = HohoemaCommnadHelper.GetPageManager();
                pageManager.OpenPage(pin.PageType, pin.Parameter);
            }
        }
    }
}
