#nullable enable
using Hohoema.Contracts.Navigations;
using System.Threading.Tasks;

namespace Hohoema.Contracts.Navigations;

public interface INavigationAware
{
    void OnNavigatedFrom(INavigationParameters parameters);
    void OnNavigatingTo(INavigationParameters parameters);
    void OnNavigatedTo(INavigationParameters parameters);
    Task OnNavigatedToAsync(INavigationParameters parameters);
}
