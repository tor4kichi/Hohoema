#nullable enable
using Hohoema.Models.PageNavigation;

namespace Hohoema.Contracts.Navigations;

public interface IPageNavigatable
{
    HohoemaPageType PageType { get; }
    INavigationParameters Parameter { get; }
}
