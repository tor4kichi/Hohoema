using Hohoema.Presentation.ViewModels.Pages.Niconico.Video;
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
using CommunityToolkit.Mvvm.DependencyInjection;
using Hohoema.Presentation.ViewModels.Pages.Hohoema;

// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace Hohoema.Presentation.Views.Pages.Niconico.Video
{
    /// <summary>
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
    /// </summary>
    public sealed partial class VideoInfomationPage : Page
    {
        public VideoInfomationPage()
        {
            this.InitializeComponent();

            Loaded += VideoInfomationPage_Loaded;
            DataContext = _vm = Ioc.Default.GetRequiredService<VideoInfomationPageViewModel>();
        }

        private readonly VideoInfomationPageViewModel _vm;

        private void VideoInfomationPage_Loaded(object sender, RoutedEventArgs e)
        {
            _LoadingCalled = false;
        }

        bool _LoadingCalled;
        private void IchibaItems_EffectiveViewportChanged(FrameworkElement sender, EffectiveViewportChangedEventArgs args)
        {
            if (!_LoadingCalled && args.BringIntoViewDistanceY <= 0)
            {
                _LoadingCalled = true;

                var vm = (DataContext as VideoInfomationPageViewModel);
                vm.InitializeIchibaItems();
                vm.InitializeRelatedVideos();
            }
        }
    }
}
