using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace NicoPlayerHohoema.Views.Helpers
{
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
}
