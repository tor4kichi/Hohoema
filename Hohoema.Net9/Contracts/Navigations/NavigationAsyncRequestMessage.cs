#nullable enable
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Hohoema.Contracts.Navigations;


public readonly struct PageNavigationEventArgs
{
    public PageNavigationEventArgs(
        string pageName,
        INavigationParameters? parameters = null,
        bool isMainViewTarget = true,
        NavigationStackBehavior behavior = NavigationStackBehavior.Push
        )
    {
        PageName = pageName;
        Paramter = parameters;
        IsMainViewTarget = isMainViewTarget;
        Behavior = behavior;
    }

    public readonly string PageName;
    public readonly INavigationParameters? Paramter;
    public readonly bool IsMainViewTarget;
    public readonly NavigationStackBehavior Behavior;
}

public enum NavigationStackBehavior
{
    Push,
    Root,
    NotRemember,
}

public class NavigationAsyncRequestMessage : AsyncRequestMessage<INavigationResult>
{
    public NavigationAsyncRequestMessage(PageNavigationEventArgs navigationRequest)
    {
        NavigationRequest = navigationRequest;
    }

    public NavigationAsyncRequestMessage(
        string pageName,
        INavigationParameters? parameters = null,
        bool isMainViewTarget = true,
        NavigationStackBehavior behavior = NavigationStackBehavior.Push
        )
    {
        NavigationRequest = new(pageName, parameters, isMainViewTarget, behavior);
    }

    public PageNavigationEventArgs NavigationRequest { get; }
}
