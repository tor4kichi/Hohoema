using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace NicoPlayerHohoema.Views.Helpers
{
    public static class SystemThemeHelper
    {
        static string _uiTheme = new Windows.UI.ViewManagement.UISettings().GetColorValue(Windows.UI.ViewManagement.UIColorType.Background).ToString();

        public static ApplicationTheme GetSystemTheme()
        {
            ApplicationTheme appTheme;
            if (_uiTheme == "#FF000000")
            {
                appTheme = ApplicationTheme.Dark;
            }
            else
            {
                appTheme = ApplicationTheme.Light;
            }

            return appTheme;
        }
    }
}
