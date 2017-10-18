using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;

namespace NicoPlayerHohoema.Helpers
{
    public static class CulturelizeHelper
    {
        static ResourceLoader _resourceLoader = ResourceLoader.GetForViewIndependentUse();

        public static string ToCulturelizeString(this string resourceName)
        {
            return _resourceLoader.GetString(resourceName);
        }

        public static string ToCulturelizeString(this Enum resourceName)
        {
            return _resourceLoader.GetString(resourceName.ToString());
        }
    }
}
