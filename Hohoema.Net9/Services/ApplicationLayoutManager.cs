﻿#nullable enable
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

namespace Hohoema.Services;

[WinRT.GeneratedBindableCustomProperty]
public sealed partial class ApplicationLayoutManager : ObservableObject
{
    private readonly AppearanceSettings _appearanceSettings;

    private ApplicationLayout _appLayout;
    public ApplicationLayout AppLayout
    {
        get { return _appLayout; }
        private set { SetProperty(ref _appLayout, value); }
    }



    private static ApplicationInteractionMode GetDefaultInteractionMode()
    {
        if (DeviceTypeHelper.IsDesktop)
        {
            return ApplicationInteractionMode.Mouse;
        }
        else if (DeviceTypeHelper.IsMobile)
        {
            return ApplicationInteractionMode.Touch;
        }
        else if (DeviceTypeHelper.IsXbox)
        {
            return ApplicationInteractionMode.Controller;
        }
        else
        {
            return ApplicationInteractionMode.Touch;
        }
    }


    public ApplicationInteractionMode DefaultInteractionMode { get; } = GetDefaultInteractionMode();

    public bool IsMouseInteractionDefault => DefaultInteractionMode == ApplicationInteractionMode.Mouse;
    public bool IsTouchInteractionDefault => DefaultInteractionMode == ApplicationInteractionMode.Touch;
    public bool IsControllerInteractionDefault => DefaultInteractionMode == ApplicationInteractionMode.Controller;



    private ApplicationInteractionMode _InteractionMode;
    public ApplicationInteractionMode InteractionMode
    {
        get { return _InteractionMode; }
        private set 
        {
            if (SetProperty(ref _InteractionMode, value))
            {
                OnPropertyChanging(nameof(IsMouseInteraction));
                OnPropertyChanged(nameof(IsMouseInteraction));
                OnPropertyChanging(nameof(IsTouchInteraction));
                OnPropertyChanged(nameof(IsTouchInteraction));
                OnPropertyChanging(nameof(IsControllerInteraction));
                OnPropertyChanged(nameof(IsControllerInteraction));
            }
        }
    }

    public bool IsMouseInteraction => _InteractionMode == ApplicationInteractionMode.Mouse;
    public bool IsTouchInteraction => _InteractionMode == ApplicationInteractionMode.Touch;
    public bool IsControllerInteraction => _InteractionMode == ApplicationInteractionMode.Controller;

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
        ApplicationInteractionMode interactionMode = ApplicationInteractionMode.Touch;
        if (_appearanceSettings.OverrideInteractionMode.HasValue)
        {
            interactionMode = _appearanceSettings.OverrideInteractionMode.Value;
        }
        else
        {
            interactionMode = DefaultInteractionMode;
        }
        return interactionMode;
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
