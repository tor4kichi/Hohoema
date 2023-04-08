using Hohoema.Models.PageNavigation;

namespace Hohoema.Contracts.Services.Navigations;

public interface IPageNavigatable
{
    HohoemaPageType PageType { get; }
    INavigationParameters Parameter { get; }
}
