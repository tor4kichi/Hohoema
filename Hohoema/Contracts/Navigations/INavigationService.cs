using System.Threading.Tasks;
using Windows.UI.Xaml.Media.Animation;

namespace Hohoema.Contracts.Services.Navigations
{
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
}
