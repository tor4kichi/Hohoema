#nullable enable
using Hohoema.Models.Pins;

namespace Hohoema.Models.PageNavigation;

public interface IPinablePage
{
    HohoemaPin GetPin();
}
