using Hohoema.Models.PageNavigation;
using Hohoema.Services.Navigations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Services.Navigations
{
    public interface IPageNavigatable
    {
        HohoemaPageType PageType { get; }
        INavigationParameters Parameter { get; }
    }
}
