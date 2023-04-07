using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Navigation;

namespace Hohoema.Navigations
{
    public static class NavigationParametersExtensions
    {
        public static bool TryGetValue<T>(this INavigationParameters parameters, string key, out T outValue)
        {
            if (!parameters.ContainsKey(key))
            {
                outValue = default(T);
                return false;
            }
            else
            {
                return parameters.TryGetValue(key, out outValue);
            }
        }

        public const string NavigationModeKey = "__nm";
        public static NavigationMode GetNavigationMode(this INavigationParameters parameters)
        {
            return parameters.TryGetValue<NavigationMode>(NavigationModeKey, out var mode) ? mode : throw new InvalidOperationException();
        }

        public static void SetNavigationMode(this INavigationParameters parameters, NavigationMode mode)
        {
            if (parameters == null) { return; }

            parameters.Remove(NavigationModeKey);
            parameters.Add(NavigationModeKey, mode);
        }
    }
}
