using Microsoft.Toolkit.Uwp.UI.Animations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace NicoPlayerHohoema.Views
{
    /// <summary>
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
    /// </summary>
    public sealed partial class SplashPage : Page
    {
        // https://docs.microsoft.com/ja-jp/windows/uwp/launch-resume/create-a-customized-splash-screen

        internal Rect _SplashImageRect; // Rect to store splash screen image coordinates.
        private SplashScreen _SplashScreen; // Variable to hold the splash screen object.
        internal bool dismissed = false; // Variable to track splash screen dismissal status.

        public SplashPage()
        {
            this.InitializeComponent();

            this.Loaded += SplashPage_Loaded;
            this.Unloaded += SplashPage_Unloaded;

            _SplashScreen = (App.Current as App).SplashScreen;
            _SplashImageRect = _SplashScreen.ImageLocation;
        }

        void PositionImage()
        {
            LogoImage.SetValue(Canvas.LeftProperty, _SplashImageRect.X);
            LogoImage.SetValue(Canvas.TopProperty, _SplashImageRect.Y);
            LogoImage.Height = _SplashImageRect.Height;
            LogoImage.Width = _SplashImageRect.Width;
        }



        private void SplashPage_Unloaded(object sender, RoutedEventArgs e)
        {
            LogoEffectAnim?.Dispose();
            Window.Current.SizeChanged -= Current_SizeChanged;
            CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = false;
        }

        AnimationSet LogoEffectAnim;
        private void SplashPage_Loaded(object sender, RoutedEventArgs e)
        {
            PositionImage();

            Window.Current.SizeChanged += Current_SizeChanged;
            
            CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = true;
            LogoEffectAnim = LogoImage.Offset(0, -64, 250, 100);
            LogoEffectAnim.Completed += (_, s) =>
            {
                LoadingUI.Fade(1.0f, 250).Start();
            };
            LogoEffectAnim.Start();
        }

        private void Current_SizeChanged(object sender, Windows.UI.Core.WindowSizeChangedEventArgs e)
        {
            _SplashImageRect = _SplashScreen.ImageLocation;
            PositionImage();
        }


    }
}
