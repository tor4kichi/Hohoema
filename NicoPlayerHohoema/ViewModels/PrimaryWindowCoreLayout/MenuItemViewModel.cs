using NicoPlayerHohoema.Interfaces;
using NicoPlayerHohoema.Services;
using Prism.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.ViewModels.PrimaryWindowCoreLayout
{
    public class MenuItemViewModel : HohoemaListingPageItemBase, IPageNavigatable
    {
        public MenuItemViewModel(string label, HohoemaPageType pageType, INavigationParameters paramaeter = null)
        {
            Label = label;
            PageType = pageType;
            Parameter = paramaeter;

            IsSelected = false;
        }

        public HohoemaPageType PageType { get; set; }
        public INavigationParameters Parameter { get; set; }
    }
}
