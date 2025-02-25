﻿#nullable enable
#nullable enable
using Hohoema.Contracts.Services;
using Hohoema.Helpers;
using Hohoema.Infra;
using Hohoema.Models.PageNavigation;
using Microsoft.UI.Xaml.Controls;
using System;
using Windows.Foundation;
using Windows.UI.Xaml;

namespace Hohoema.Models.Application;

public class AppearanceSettings : FlagsRepositoryBase
{
    public AppearanceSettings(ILocalizeService localizeService)
    {
        _autoRestartOnUpdateInstalled = Read(true, nameof(AutoRestartOnUpdateInstalled));
        _locale = Read(localizeService.GetDefaultLocale(), nameof(Locale));
        _firstAppearPageType = Read(HohoemaPageType.RankingCategoryList, nameof(FirstAppearPageType));
        _OverrideInteractionMode = Read(default(ApplicationInteractionMode?), nameof(OverrideInteractionMode));
        _Theme = Read(Internal_ElementTheme.Default, nameof(ApplicationTheme)) switch
        {
            Internal_ElementTheme.Default => ElementTheme.Default,
            Internal_ElementTheme.Light => ElementTheme.Light,
            Internal_ElementTheme.Dark => ElementTheme.Dark,
            _ => throw new NotSupportedException()
        };

        _menuPaneDisplayMode = Read(DeviceTypeHelper.IsXbox ? Internal_PaneDisplayMode.LeftMinimal : Internal_PaneDisplayMode.Auto, nameof(MenuPaneDisplayMode)) switch
        {
            Internal_PaneDisplayMode.Auto => NavigationViewPaneDisplayMode.Auto,
            Internal_PaneDisplayMode.Left => NavigationViewPaneDisplayMode.Left,
            Internal_PaneDisplayMode.Top => NavigationViewPaneDisplayMode.Top,
            Internal_PaneDisplayMode.LeftCompact => NavigationViewPaneDisplayMode.LeftCompact,
            Internal_PaneDisplayMode.LeftMinimal => NavigationViewPaneDisplayMode.LeftMinimal,
            _ => throw new NotSupportedException()
        };

        _PlayerDisplayView = Read(PlayerDisplayView.PrimaryView, nameof(PlayerDisplayView));
        _IsSecondaryViewPrefferedCompactOverlay = Read(false, nameof(IsSecondaryViewPrefferedCompactOverlay));
        _SecondaryViewDisplayRegionMonitorDeviceId = Read(string.Empty, nameof(SecondaryViewDisplayRegionMonitorDeviceId));
        _SecondaryViewLastWindowPosition = Read(default(Point?), nameof(SecondaryViewLastWindowPosition));
        _SecondaryViewLastWindowSize = Read(default(Size?), nameof(SecondaryViewLastWindowSize));        
    }

    private bool _autoRestartOnUpdateInstalled;
    public bool AutoRestartOnUpdateInstalled
    {
        get => _autoRestartOnUpdateInstalled;
        set => SetProperty(ref _autoRestartOnUpdateInstalled, value);
    }

    private string _locale;
    public string Locale
    {
        get => _locale;
        set => SetProperty(ref _locale, value);
    }

    private HohoemaPageType _firstAppearPageType;
    public HohoemaPageType FirstAppearPageType
    {
        get => _firstAppearPageType;
        set => SetProperty(ref _firstAppearPageType, value);
    }

    private ApplicationInteractionMode? _OverrideInteractionMode;    
    public ApplicationInteractionMode? OverrideInteractionMode
    {
        get => _OverrideInteractionMode;
        set => SetProperty(ref _OverrideInteractionMode, value);
    }

    // Themeは他で利用してるためかシリアライズエラーが発生するのでApplicationThemeとしている
    private ElementTheme _Theme;
    public ElementTheme ApplicationTheme
    {
        get => _Theme;
        set
        {
            if (_Theme != value)
            {
                Internal_ElementTheme internal_theme = value switch
                {
                    ElementTheme.Default => Internal_ElementTheme.Default,
                    ElementTheme.Light => Internal_ElementTheme.Light,
                    ElementTheme.Dark => Internal_ElementTheme.Dark,
                    _ => throw new NotSupportedException()
                };
                Save(internal_theme);

                _Theme = value;
                OnPropertyChanged();
            }
        }
    }

    private enum Internal_ElementTheme
    {
        Default,
        Light,
        Dark,
    }



    private NavigationViewPaneDisplayMode _menuPaneDisplayMode;
    public NavigationViewPaneDisplayMode MenuPaneDisplayMode
    {
        get => _menuPaneDisplayMode;
        set
        {
            if (_menuPaneDisplayMode != value)
            {
                Internal_PaneDisplayMode internal_theme = value switch
                {
                    NavigationViewPaneDisplayMode.Auto => Internal_PaneDisplayMode.Auto,
                    NavigationViewPaneDisplayMode.Left => Internal_PaneDisplayMode.Left,
                    NavigationViewPaneDisplayMode.Top => Internal_PaneDisplayMode.Top,
                    NavigationViewPaneDisplayMode.LeftCompact => Internal_PaneDisplayMode.LeftCompact,
                    NavigationViewPaneDisplayMode.LeftMinimal => Internal_PaneDisplayMode.LeftMinimal,
                    _ => throw new NotSupportedException()
                };
                Save(internal_theme);

                _menuPaneDisplayMode = value;
                OnPropertyChanged();
            }
        }
    }

    private enum Internal_PaneDisplayMode
    {
        Auto = 0,
        //
        // 概要:
        //     The pane is shown on the left side of the control in its fully open state.
        Left = 1,
        //
        // 概要:
        //     The pane is shown at the top of the control.
        Top = 2,
        //
        // 概要:
        //     The pane is shown on the left side of the control. Only the pane icons are shown
        //     by default.
        LeftCompact = 3,
        //
        // 概要:
        //     The pane is shown on the left side of the control. Only the pane menu button
        //     is shown by default.
        LeftMinimal = 4
    }



    private PlayerDisplayView _PlayerDisplayView;
    public PlayerDisplayView PlayerDisplayView
    {
        get => _PlayerDisplayView;
        set => SetProperty(ref _PlayerDisplayView, value);
    }

    private bool _IsSecondaryViewPrefferedCompactOverlay;
    public bool IsSecondaryViewPrefferedCompactOverlay
    {
        get => _IsSecondaryViewPrefferedCompactOverlay;
        set => SetProperty(ref _IsSecondaryViewPrefferedCompactOverlay, value);
    }

    private string _SecondaryViewDisplayRegionMonitorDeviceId;    
    public string SecondaryViewDisplayRegionMonitorDeviceId
    {
        get => _SecondaryViewDisplayRegionMonitorDeviceId;
        set => SetProperty(ref _SecondaryViewDisplayRegionMonitorDeviceId, value);
    }

    private Point? _SecondaryViewLastWindowPosition;
    public Point? SecondaryViewLastWindowPosition
    {
        get => _SecondaryViewLastWindowPosition;
        set => SetProperty(ref _SecondaryViewLastWindowPosition, value);
    }

    private Size? _SecondaryViewLastWindowSize;
    public Size? SecondaryViewLastWindowSize
    {
        get => _SecondaryViewLastWindowSize;
        set => SetProperty(ref _SecondaryViewLastWindowSize, value);
    }

    public bool IsVideoListItemDoubleClickOrDoubleTapToPlayEnabled
    {
        get => Read(false);
        set => Save(value);
    }

    public bool IsVideoListItemAdditionalUIEnabled
    {
        get => Read(true);
        set => Save(value);
    }

    public bool IsVideoListItemMiddleClickToAddQueueEnabled
    {
        get => Read(true);
        set => Save(value);
    }

    public bool IsVideoListThumbnailCacheEnabled
    {
        get => Read(true);
        set => Save(value);
    }

    public int VideoListThumbnailCacheMaxCount
    {
        get => Read(1000);
        set => Save(value);
    }
}

public enum PlayerSizeMode
{
    Default,
    FullScreen,
    CompactOverlay,
}
