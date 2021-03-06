﻿
using Hohoema.Models.Domain.PageNavigation;
using Hohoema.Presentation.Services;
using Prism.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Presentation.ViewModels.PrimaryWindowCoreLayout
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
