#nullable enable
using Windows.UI.Xaml;

namespace Hohoema.Views.Helpers;

public static class VisibilityExtension
{
    public static Visibility ToVisibility(this bool boolean)
    {
        return boolean ? Visibility.Visible : Visibility.Collapsed;
    }

    public static Visibility ToInvisibility(this bool boolean)
    {
        return ToVisibility(!boolean);
    }
}
