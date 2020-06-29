using Hohoema.Models.Pages;
using Hohoema.Services;
using Prism.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.ViewModels.Pages
{
    public interface IPageNavigatable
    {
        HohoemaPageType PageType { get; }
        INavigationParameters Parameter { get; }
    }
}
