using CommunityToolkit.Mvvm.ComponentModel;
using Hohoema.Helpers;
using Hohoema.Models.Application;
using Microsoft.UI.Xaml.Controls;
using Reactive.Bindings.Extensions;
using System;
using System.Linq;
using System.Reactive.Linq;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Xamarin.Essentials;

namespace Hohoema.Services;

public sealed class ApplicationLayoutManager : ObservableObject
{
    private readonly AppearanceSettings _appearanceSettings;

    private ApplicationLayout _appLayout;
    public ApplicationLayout AppLayout
    {
        get { return _appLayout; }
        private set { SetProperty(ref _appLayout, value); }
    }

    private ApplicationInteractionMode _InteractionMode;
    public ApplicationInteractionMode InteractionMode
    {
        get { return _InteractionMode; }
        private set { SetProperty(ref _InteractionMode, value); }
    }

    private NavigationViewDisplayMode _navigationViewDisplayMode;
    public NavigationViewDisplayMode NavigationViewDisplayMode
    {
        get { return _navigationViewDisplayMode; }
        private set { SetProperty(ref _navigationViewDisplayMode, value); }
    }



    internal void SetCurrentNavigationViewDisplayMode(NavigationViewDisplayMode newDisplayMode)
    {
        NavigationViewDisplayMode = newDisplayMode;
    }


    private NavigationViewPaneDisplayMode _navigationViewPaneDisplayMode;
    public NavigationViewPaneDisplayMode NavigationViewPaneDisplayMode
    {
        get { return _navigationViewPaneDisplayMode; }
        private set { SetProperty(ref _navigationViewPaneDisplayMode, value); }
    }

    internal void SetCurrentNavigationViewPaneDisplayMode(NavigationViewPaneDisplayMode newPaneDisplayMode)
    {
        NavigationViewPaneDisplayMode = newPaneDisplayMode;
    }

    private NavigationViewBackButtonVisible _navigationViewIsBackButtonVisible;
    public NavigationViewBackButtonVisible NavigationViewIsBackButtonVisible
    {
        get { return _navigationViewIsBackButtonVisible; }
        private set { SetProperty(ref _navigationViewIsBackButtonVisible, value); }
    }

    internal void SetCurrentNavigationViewIsBackButtonVisible(NavigationViewBackButtonVisible visibility)
    {
        NavigationViewIsBackButtonVisible = visibility;
    }


    public ApplicationLayoutManager(AppearanceSettings appearanceSettings)
    {
        _appearanceSettings = appearanceSettings;

        new[]
        {
            _appearanceSettings.ObserveProperty(x => x.OverrideInteractionMode).ToUnit(),
            Observable.FromEventPattern<WindowSizeChangedEventHandler, WindowSizeChangedEventArgs>(
                h => Window.Current.SizeChanged += h,
                h => Window.Current.SizeChanged -= h
                ).ToUnit()
        }
        .Merge()
        .Subscribe(_ => RefreshAppLayout());            
    }

    void RefreshAppLayout()
    {
        InteractionMode = GetInteractionMode();
        AppLayout = GetAppLayout(InteractionMode);
        System.Diagnostics.Debug.WriteLine($"InteractionMode: {InteractionMode}, ApplicationLayout: {AppLayout} (override: {_appearanceSettings.OverrideInteractionMode.HasValue})");
    }

    ApplicationInteractionMode GetInteractionMode()
    {
        ApplicationInteractionMode intaractionMode = ApplicationInteractionMode.Touch;
        if (_appearanceSettings.OverrideInteractionMode.HasValue)
        {
            intaractionMode = _appearanceSettings.OverrideInteractionMode.Value;
        }
        else
        {
            if (DeviceInfo.Idiom == DeviceIdiom.Desktop)
            {
                intaractionMode = ApplicationInteractionMode.Mouse;
            }
            else if (DeviceInfo.Idiom == DeviceIdiom.Phone || DeviceInfo.Idiom == DeviceIdiom.Tablet)
            {
                intaractionMode = ApplicationInteractionMode.Touch;
            }
            else if (DeviceInfo.Idiom == DeviceIdiom.TV || DeviceTypeHelper.IsXbox)
            {
                intaractionMode = ApplicationInteractionMode.Controller;
            }
        }
        return intaractionMode;
    }

    ApplicationLayout GetAppLayout(ApplicationInteractionMode intaractionMode)
    {
        ApplicationLayout layout = ApplicationLayout.Mobile;
        if (intaractionMode == ApplicationInteractionMode.Mouse
            || intaractionMode == ApplicationInteractionMode.Touch
            )
        {
            var width = Window.Current.Bounds.Width;
            if (width <= 519)
            {
                layout = ApplicationLayout.Mobile;
            }
            else if (width <= 799)
            {
                layout = ApplicationLayout.Tablet;
            }
            else
            {
                layout = ApplicationLayout.Desktop;
            }
        }
        else if (intaractionMode == ApplicationInteractionMode.Controller)
        {
            layout = ApplicationLayout.TV;
        }

        return layout;
    }
}
