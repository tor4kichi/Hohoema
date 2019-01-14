using NicoPlayerHohoema.Models;
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
using Unity;
using Prism.Unity;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace NicoPlayerHohoema.Views
{
    public sealed partial class PlayerWithPageContainer : ContentControl
    {
        public Frame Frame
        {
            get { return (Frame)GetValue(FrameProperty); }
            set { SetValue(FrameProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FrameProperty =
            DependencyProperty.Register(nameof(Frame), typeof(Frame), typeof(PlayerWithPageContainer), new PropertyMetadata(default(Frame)));


        public PlayerWithPageContainer()
        {
            this.InitializeComponent();

            Loaded += PlayerWithPageContainer_Loaded;
            Loading += PlayerWithPageContainer_Loading;
        }

        private void PlayerWithPageContainer_Loading(FrameworkElement sender, object args)
        {
            DataContext = App.Current.Container.Resolve<ViewModels.PlayerWithPageContainerViewModel>();
        }

        private void PlayerWithPageContainer_Loaded(object sender, RoutedEventArgs e)
        {
            // タイトルバーのハンドルできる範囲を自前で指定する
            // バックボタンのカスタマイズ対応のため
            // もしかしてモバイルやXboxOneで例外が出てクラッシュするのが怖いので
            // 例外を握りつぶしておく
            try
            {
                Window.Current.SetTitleBar(GetTemplateChild("DraggableContent") as UIElement);
            }
            catch { }

            
        }

        protected override void OnApplyTemplate()
        {
            Frame = GetTemplateChild("PlayerFrame") as Frame;

            base.OnApplyTemplate();
        }
    }
    

}
