#nullable enable
using System.Threading.Tasks;
using Windows.UI.Xaml.Media.Animation;

namespace Hohoema.Contracts.Navigations;

public interface INavigationService
{
    bool CanGoForward();
    bool CanGoBack();
    Task GoForwardAsync();
    Task GoBackAsync();
    Task<INavigationResult> NavigateAsync(string pageName);
    Task<INavigationResult> NavigateAsync(string pageName, NavigationTransitionInfo infoOverride);
    Task<INavigationResult> NavigateAsync(string pageName, INavigationParameters parameters, NavigationTransitionInfo infoOverride = null);
}

public static class NavigationServiceExtensions
{
    public static Task<INavigationResult> NavigateAsync(this INavigationService ns, string pageName, params (string key, object value)[] parameters)
    {
        return ns.NavigateAsync(pageName, new NavigationParameters(parameters));
    }
}