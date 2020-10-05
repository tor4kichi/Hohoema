using Hohoema.Presentation.ViewModels.Pages.UserFeaturePages;
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

// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace Hohoema.Presentation.Views.Pages.UserFeaturePages
{
    /// <summary>
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
    /// </summary>
    public sealed partial class NicoRepoPage : Page
    {

        public double ScrollPosition
        {
            get { return (double)GetValue(ScrollPositionProperty); }
            set { SetValue(ScrollPositionProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ScrollPosition.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ScrollPositionProperty =
            DependencyProperty.Register("ScrollPosition", typeof(double), typeof(NicoRepoPage), new PropertyMetadata(0.0));


        public void ResetScrollPosition()
        {
            var scrollViweer = ItemsList.FindFirstChild<ScrollViewer>();
            scrollViweer.ChangeView(null, 0, null);
        }



        public NicoRepoPage()
        {
            this.InitializeComponent();
        }

        private void TopicsEditFlyout_Closed(object sender, object e)
        {
            if (DataContext is NicoRepoPageViewModel vm)
            {
                vm.OnResetNicoRepoItemTopicsEditCompleted();
            }
        }
    }
}
