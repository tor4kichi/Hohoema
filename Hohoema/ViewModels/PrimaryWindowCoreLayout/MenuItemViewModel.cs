
using Hohoema.Models.PageNavigation;

namespace Hohoema.ViewModels.PrimaryWindowCoreLayout;

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
