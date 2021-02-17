using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Hohoema.Models.Domain.PageNavigation;
using Hohoema.Models.Infrastructure;
using Microsoft.UI.Xaml.Controls;
using Hohoema.Presentation.Services.Helpers;

namespace Hohoema.Models.Domain.Application
{
    public class AppearanceSettings : FlagsRepositoryBase
    {
        public AppearanceSettings()
        {
            _locale = Read(I18NPortable.I18N.Current.GetDefaultLocale(), nameof(Locale));
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
        }

        private string _locale;
        public string Locale
        {
            get { return _locale; }
            set { SetProperty(ref _locale, value); }
        }

        private HohoemaPageType _firstAppearPageType;
        public HohoemaPageType FirstAppearPageType
        {
            get { return _firstAppearPageType; }
            set { SetProperty(ref _firstAppearPageType, value); }
        }

        private ApplicationInteractionMode? _OverrideInteractionMode;
        public ApplicationInteractionMode? OverrideInteractionMode
        {
            get { return _OverrideInteractionMode; }
            set { SetProperty(ref _OverrideInteractionMode, value); }
        }

        // Themeは他で利用してるためかシリアライズエラーが発生するのでApplicationThemeとしている
        private ElementTheme _Theme;
        public ElementTheme ApplicationTheme
        {
            get { return _Theme; }
            set 
            {
                if (_Theme != value)
                {
                    var internal_theme = value switch
                    {
                        ElementTheme.Default => Internal_ElementTheme.Default,
                        ElementTheme.Light => Internal_ElementTheme.Light,
                        ElementTheme.Dark => Internal_ElementTheme.Dark,
                        _ => throw new NotSupportedException()
                    };
                    Save(internal_theme);

                    _Theme = value;
                    RaisePropertyChanged();
                }
            }
        }


        enum Internal_ElementTheme
        {
            Default,
            Light,
            Dark,
        }



        private NavigationViewPaneDisplayMode _menuPaneDisplayMode;
        public NavigationViewPaneDisplayMode MenuPaneDisplayMode
        {
            get { return _menuPaneDisplayMode; }
            set
            {
                if (_menuPaneDisplayMode != value)
                {
                    var internal_theme = value switch
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
                    RaisePropertyChanged();
                }
            }
        }

        enum Internal_PaneDisplayMode
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
    }

    
}
