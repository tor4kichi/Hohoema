using Hohoema.Models.PageNavigation;
using Hohoema.Contracts.Services.Navigations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Contracts.Services.Navigations
{
    public interface IPageNavigatable
    {
        HohoemaPageType PageType { get; }
        INavigationParameters Parameter { get; }
    }
}
