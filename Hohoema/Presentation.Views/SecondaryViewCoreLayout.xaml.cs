using Hohoema.Models.Domain;
using Prism.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Prism.Ioc;
using Reactive.Bindings.Extensions;
using Windows.UI.ViewManagement;
using Windows.UI;
using Hohoema.Models.Domain.Application;
using Prism.Events;
using Hohoema.Presentation.Services.LiteNotification;
using System.Threading;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Hohoema.Presentation.Views
{
    public sealed partial class SecondaryViewCoreLayout : UserControl
    {
        public SecondaryViewCoreLayout()
        {
            this.InitializeComponent();

            var appearanceSettings = App.Current.Container.Resolve<AppearanceSettings>();
            appearanceSettings.ObserveProperty(x => x.ApplicationTheme)
                .Subscribe(theme =>
                {
                    ThemeChanged(theme);
                });

            _eventAggregator = App.Current.Container.Resolve<IEventAggregator>();
            Loaded += SecondaryViewCoreLayout_Loaded;
            Unloaded += SecondaryViewCoreLayout_Unloaded;
        }

        IEventAggregator _eventAggregator;

        IDisposable _liteNotificationEventSubscriber;
        private void SecondaryViewCoreLayout_Unloaded(object sender, RoutedEventArgs e)
        {
            _liteNotificationEventSubscriber.Dispose();
            _liteNotificationEventSubscriber = null;
        }

        private void SecondaryViewCoreLayout_Loaded(object sender, RoutedEventArgs e)
        {
            var currentContext = SynchronizationContext.Current;
            _liteNotificationEventSubscriber = _eventAggregator.GetEvent<LiteNotificationEvent>()
                .Subscribe(args => 
                {
                    if (currentContext != SynchronizationContext.Current)
                    {
                        return;
                    }

                    TimeSpan duration = args.Duration ?? args.DisplayDuration switch
                    {
                        DisplayDuration.Default => TimeSpan.FromSeconds(0.75),
                        DisplayDuration.MoreAttention => TimeSpan.FromSeconds(3),
                        _ => TimeSpan.FromSeconds(0.75),
                    };

                    LiteInAppNotification.Show(args, duration);
                }, keepSubscriberReferenceAlive: true);
        }


        void ThemeChanged(ElementTheme theme)
        {
            ApplicationTheme appTheme;
            if (theme == ElementTheme.Default)
            {
                appTheme = Helpers.SystemThemeHelper.GetSystemTheme();
                if (appTheme == ApplicationTheme.Dark)
                {
                    theme = ElementTheme.Dark;
                }
                else
                {
                    theme = ElementTheme.Light;
                }
            }
            else if (theme == ElementTheme.Dark)
            {
                appTheme = ApplicationTheme.Dark;
            }
            else
            {
                appTheme = ApplicationTheme.Light;
            }

            this.RequestedTheme = theme;

            var appView = ApplicationView.GetForCurrentView();
            if (appTheme == ApplicationTheme.Light)
            {
                appView.TitleBar.ButtonForegroundColor = Colors.Black;
                appView.TitleBar.ButtonHoverBackgroundColor = Colors.DarkGray;
                appView.TitleBar.ButtonHoverForegroundColor = Colors.Black;
                appView.TitleBar.ButtonInactiveForegroundColor = Colors.Gray;
            }
            else
            {
                appView.TitleBar.ButtonForegroundColor = Colors.White;
                appView.TitleBar.ButtonHoverBackgroundColor = Colors.DimGray;
                appView.TitleBar.ButtonHoverForegroundColor = Colors.White;
                appView.TitleBar.ButtonInactiveForegroundColor = Colors.DarkGray;
            }
        }


        public INavigationService CreateNavigationService()
        {
            return NavigationService.Create(ContentFrame);
        }
    }
}
