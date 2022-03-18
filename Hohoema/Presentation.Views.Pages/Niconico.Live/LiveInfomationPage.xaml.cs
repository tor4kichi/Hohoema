using Hohoema.Presentation.ViewModels.Pages.Niconico.Live;
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
using Hohoema.Presentation.Navigations;
using Microsoft.Toolkit.Mvvm.DependencyInjection;

// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace Hohoema.Presentation.Views.Pages.Niconico.Live
{
    /// <summary>
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
    /// </summary>
    public sealed partial class LiveInfomationPage : Page
    {
        public LiveInfomationPage()
        {
            this.InitializeComponent();

            Loaded += LiveInfomationPage_Loaded;
            DataContext = _vm = Ioc.Default.GetRequiredService<LiveInfomationPageViewModel>();
        }

        private readonly LiveInfomationPageViewModel _vm;

        private void LiveInfomationPage_Loaded(object sender, RoutedEventArgs e)
        {
            _LoadingCalled = false;
        }

        bool _LoadingCalled;
        private void Grid_EffectiveViewportChanged(FrameworkElement sender, EffectiveViewportChangedEventArgs args)
        {
            if (!_LoadingCalled && args.BringIntoViewDistanceY <= 0)
            {
                _LoadingCalled = true;

                var vm = (DataContext as LiveInfomationPageViewModel);
                vm.InitializeIchibaItems();
                vm.InitializeLiveRecommend();
            }
        }
    }
}
