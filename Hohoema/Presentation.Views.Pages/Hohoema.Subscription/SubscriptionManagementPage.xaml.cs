using Hohoema.Presentation.ViewModels.Pages.Hohoema.Subscription;
using Hohoema.Presentation.Views.Flyouts;
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
using CommunityToolkit.Mvvm.DependencyInjection;

// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace Hohoema.Presentation.Views.Pages.Hohoema.Subscription
{
    /// <summary>
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
    /// </summary>
    public sealed partial class SubscriptionManagementPage : Page
    {
        public SubscriptionManagementPage()
        {
            this.InitializeComponent();
            DataContext = _vm = Ioc.Default.GetRequiredService<SubscriptionManagementPageViewModel>();
        }

        private readonly SubscriptionManagementPageViewModel _vm;

        private void SubscriptionVideoList_ContextRequested(UIElement sender, ContextRequestedEventArgs args)
        {
            if (sender == args.OriginalSource) { return; }

            if (sender is ListViewBase listViewBase)
            {
                if (args.OriginalSource is SelectorItem selectorItem)
                {
                    selectorItem.DataContext = selectorItem.Content;
                }
                
                var subscVM = (listViewBase.DataContext as SubscriptionViewModel);
                var flyout = new VideoItemFlyout()
                {
                    SourceVideoItems = subscVM.Videos,
                    AllowSelection = false,
                };

                flyout.ShowAt(args.OriginalSource as FrameworkElement);
                args.Handled = true;
            }
        }

        private void ListView_ItemClick(object sender, ItemClickEventArgs e)
        {

        }
    }
}
