#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

namespace Hohoema.Services.Navigations;



public static class NavigationServiceExtensions
{
    public static Task<INavigationResult> NavigateAsync(this INavigationService ns, string pageName, params (string key, object value)[] parameters)
    {
        return ns.NavigateAsync(pageName, new NavigationParameters(parameters));
    }
}

public sealed class NavigationService : INavigationService
{
    public static Func<string, Type> ViewTypeResolver;

    public static NavigationService Create(Frame frame)
    {
        return new NavigationService(frame, rememberHistory: true);
    }

    public static NavigationService CreateWithoutHistory(Frame frame)
    {
        return new NavigationService(frame, rememberHistory: false);
    }


    public Frame Frame { get; }

    //private List<INavigationParameters> BackParametersStack = new List<INavigationParameters>();
    //private List<INavigationParameters> ForwardParametersStack = new List<INavigationParameters>();

    private INavigationParameters CurrentPageParameters;
    private readonly bool _rememberHistory;


    public (Type PageType, INavigationParameters Parameters) GetCurrentPage()
    {
        return (Frame.Content.GetType(), CurrentPageParameters);
    }

    public IEnumerable<(Type PageType, INavigationParameters Parameters)> GetBackStackPages()
    {
        foreach (var depth in Enumerable.Range(0, Frame.BackStackDepth))
        {
            var pageEntry = Frame.BackStack[depth];
            yield return (pageEntry.SourcePageType, pageEntry.Parameter as INavigationParameters);
        }            
    }



    private NavigationService(Frame frame, bool rememberHistory)
    {
        Frame = frame;
        _rememberHistory = rememberHistory;
    }

    public bool CanGoBack()
    {
        if (_rememberHistory is false) { return false; }

        return Frame.CanGoBack;
    }

    public bool CanGoForward()
    {
        if (_rememberHistory is false) { return false; }

        return Frame.CanGoForward;
    }

    public async Task GoForwardAsync()
    {
        if (_rememberHistory is false) { return; }
        if (Frame.CanGoForward is false) { return; }

        var forwardPageEntry = Frame.ForwardStack.Last();
        var prevPage = Frame.Content as Page;
        Frame.GoForward();
        var forwardNavigationParameters = forwardPageEntry.Parameter as INavigationParameters;
        forwardNavigationParameters.SetNavigationMode(NavigationMode.Forward);
        var currentPage = Frame.Content as Page;
        await HandleViewModelNavigation(prevPage?.DataContext as INavigationAware, currentPage?.DataContext as INavigationAware, forwardNavigationParameters);
    }

    public async Task GoBackAsync()
    {
        if (_rememberHistory is false) { return; }
        if (Frame.CanGoBack is false) { return; }

        var backPageEntry = Frame.BackStack.Last();
        var prevPage = Frame.Content as Page;
        Frame.GoBack();

        var lastNavigationParameters = backPageEntry.Parameter as INavigationParameters;
        lastNavigationParameters.SetNavigationMode(NavigationMode.Back);
        var currentPage = Frame.Content as Page;
        await HandleViewModelNavigation(prevPage?.DataContext as INavigationAware, currentPage?.DataContext as INavigationAware, lastNavigationParameters);
    }

    public Task<INavigationResult> NavigateAsync(string pageName, INavigationParameters parameters, NavigationTransitionInfo infoOverride)
    {
        return Internal_NavigationAsync(pageName, parameters, infoOverride);
    }

    public Task<INavigationResult> NavigateAsync(string pageName, INavigationParameters parameters)
    {
        return Internal_NavigationAsync(pageName, parameters, null);
    }

    public Task<INavigationResult> NavigateAsync(string pageName, NavigationTransitionInfo infoOverride)
    {
        return Internal_NavigationAsync(pageName, null, infoOverride);
    }

    public Task<INavigationResult> NavigateAsync(string pageName)
    {
        return Internal_NavigationAsync(pageName, null, null);
    }

    private async Task<INavigationResult> Internal_NavigationAsync(string pageName, INavigationParameters parameters, NavigationTransitionInfo infoOverride)
    {
        var prevPage = Frame.Content as Page;

        parameters ??= new NavigationParameters();
        var viewType = ViewTypeResolver(pageName);
        if (Frame.Navigate(viewType, parameters, infoOverride))
        {
            if (_rememberHistory is false)
            {
                Frame.BackStack.Clear();
            }

            var currentPage = Frame.Content as Page;
            parameters.SetNavigationMode(NavigationMode.New);
            return await HandleViewModelNavigation(prevPage?.DataContext as INavigationAware, currentPage.DataContext as INavigationAware, parameters);
        }
        else
        {
            return new NavigationResult() { IsSuccess = false };
        }
    }



    private async Task<NavigationResult> HandleViewModelNavigation(INavigationAware fromPageVM, INavigationAware toPageVM, INavigationParameters parameters)
    {            
        if (fromPageVM != null)
        {
            try
            {
                fromPageVM.OnNavigatedFrom(parameters);
            }
            catch (Exception ex)
            {
                return new NavigationResult() { IsSuccess = false, Exception = ex };
            }
        }


        if (toPageVM != null)
        {
            try
            {
                toPageVM.OnNavigatingTo(parameters);
                toPageVM.OnNavigatedTo(parameters);
                await toPageVM.OnNavigatedToAsync(parameters);
            }
            catch (Exception ex)
            {
                return new NavigationResult() { IsSuccess = false, Exception = ex };
            }
        }

        return new NavigationResult() { IsSuccess = true };

    }

}
