using Hohoema.Models.Domain.Application;
using Hohoema.Presentation.Services.LiteNotification;
using Microsoft.Toolkit.Mvvm.Messaging;
using Prism.Ioc;
using Prism.Navigation;
using Reactive.Bindings.Extensions;
using System;
using System.Threading;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

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

            Loaded += SecondaryViewCoreLayout_Loaded;
            Unloaded += SecondaryViewCoreLayout_Unloaded;
        }

        IDisposable _liteNotificationEventSubscriber;
        private void SecondaryViewCoreLayout_Unloaded(object sender, RoutedEventArgs e)
        {
            _liteNotificationEventSubscriber.Dispose();
            _liteNotificationEventSubscriber = null;

            StrongReferenceMessenger.Default.Unregister<LiteNotificationMessage>(this);
        }

        private void SecondaryViewCoreLayout_Loaded(object sender, RoutedEventArgs e)
        {
            var currentContext = SynchronizationContext.Current;
            StrongReferenceMessenger.Default.Register<LiteNotificationMessage>(this, (r, m) => 
                {
                    if (currentContext != SynchronizationContext.Current)
                    {
                        return;
                    }

                    TimeSpan duration = m.Value.Duration ?? m.Value.DisplayDuration switch
                    {
                        DisplayDuration.Default => TimeSpan.FromSeconds(0.75),
                        DisplayDuration.MoreAttention => TimeSpan.FromSeconds(3),
                        _ => TimeSpan.FromSeconds(0.75),
                    };

                    LiteInAppNotification.Show(m.Value, duration);
                });
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
