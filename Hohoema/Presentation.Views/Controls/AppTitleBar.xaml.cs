using Hohoema.Models.Domain.Application;
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

// ユーザー コントロールの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234236 を参照してください

namespace Hohoema.Presentation.Views.Controls
{
    public sealed partial class AppTitleBar : UserControl
    {
        private static AppearanceSettings _AppearanceSettings { get; }

        static AppTitleBar()
        {
            _AppearanceSettings = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<AppearanceSettings>();
        }

        private AppearanceSettings AppearanceSettings => _AppearanceSettings;

        public AppTitleBar()
        {
            this.InitializeComponent();

            Loaded += AppTitleBar_Loaded;
        }

        private void AppTitleBar_Loaded(object sender, RoutedEventArgs e)
        {
            if (_AppearanceSettings.MenuPaneDisplayMode is Microsoft.UI.Xaml.Controls.NavigationViewPaneDisplayMode.LeftMinimal)
            {
                TitleText.Margin = new Thickness(48, 0, 0, 0);
                TitleText.FontSize = 16;
            }
            else
            {
                TitleText.Margin = new Thickness(0, 0, 0, 0);
                TitleText.FontSize = 16;
            }
        }

        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Title.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(AppTitleBar), new PropertyMetadata(string.Empty));





        public string SubTitle
        {
            get { return (string)GetValue(SubTitleProperty); }
            set { SetValue(SubTitleProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SubTitle.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SubTitleProperty =
            DependencyProperty.Register("SubTitle", typeof(string), typeof(AppTitleBar), new PropertyMetadata(string.Empty));
        
    }
}
