
using Hohoema.Models.PageNavigation;
using Hohoema.Services.Navigations;
using Hohoema.Services.Navigations;
using Hohoema.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.ViewModels.PrimaryWindowCoreLayout
{
    public class MenuItemViewModel : HohoemaListingPageItemBase
    {
        public MenuItemViewModel(string label)
        {
            Label = label;

            IsSelected = false;
        }

    }

    public class NavigateAwareMenuItemViewModel : MenuItemViewModel, IPageNavigatable
    {
        public NavigateAwareMenuItemViewModel(string label, HohoemaPageType pageType, INavigationParameters paramaeter = null)
            : base(label)
        {
            PageType = pageType;
            Parameter = paramaeter;

            IsSelected = false;
        }

        public HohoemaPageType PageType { get; set; }
        public INavigationParameters Parameter { get; set; }
    }



}
