using Hohoema.Models.Domain.Application;
using Hohoema.Models.Domain.Notification;
using Hohoema.Presentation.Services;
using Microsoft.Toolkit.Mvvm.Messaging;
using Hohoema.Presentation.Navigations;
using Reactive.Bindings.Extensions;
using System;
using System.Reactive.Disposables;
using System.Threading;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Hohoema.Presentation.Views.Pages
{
    public sealed partial class SecondaryWindowCoreLayout : UserControl
    {
        public SecondaryWindowCoreLayout()
        {
            this.InitializeComponent();

           
            _CurrentActiveWindowUIContextService = Microsoft.Toolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<Services.CurrentActiveWindowUIContextService>();

            Loaded += SecondaryViewCoreLayout_Loaded;
            Unloaded += SecondaryViewCoreLayout_Unloaded;
        }

        CompositeDisposable _disposables;

        private readonly CurrentActiveWindowUIContextService _CurrentActiveWindowUIContextService;

        private void SecondaryViewCoreLayout_Unloaded(object sender, RoutedEventArgs e)
        {
            _disposables.Dispose();

            WeakReferenceMessenger.Default.Unregister<LiteNotificationMessage>(this);
        }

        private void SecondaryViewCoreLayout_Loaded(object sender, RoutedEventArgs e)
        {
            var appearanceSettings = Microsoft.Toolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<AppearanceSettings>();           
            _disposables = new CompositeDisposable(new[] 
            {
                appearanceSettings.ObserveProperty(x => x.ApplicationTheme)
                .Subscribe(theme =>
                {
                    ThemeChanged(theme);
                })
            });

            WeakReferenceMessenger.Default.Register<LiteNotificationMessage>(this, (r, m) => 
            {
                if (_CurrentActiveWindowUIContextService.UIContext != UIContext)
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
