using Hohoema.Presentation.Services;
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
using Hohoema.Models.Domain.Notification;
using Microsoft.Toolkit.Mvvm.DependencyInjection;
using Hohoema.Presentation.ViewModels.Pages.Hohoema;

// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace Hohoema.Presentation.Views.Pages.Hohoema
{
    /// <summary>
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
    /// </summary>
    public sealed partial class DebugPage : Page
    {
        public DebugPage()
        {
            this.InitializeComponent();

            DataContext = Ioc.Default.GetRequiredService<DebugPageViewModel>();
        }

        private void ForceThrowException(object sender, RoutedEventArgs e)
        {
            throw new Models.Infrastructure.HohoemaExpception("例外テスト");
        }

        private void TestCrashReport_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void TestInAppNotification(object sender, RoutedEventArgs e)
        {
            var notificationService = Microsoft.Toolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<NotificationService>();
            notificationService.ShowInAppNotification(new InAppNotificationPayload() 
            {
                Title = "通知テスト",
                Content = "通知テスト\nあああああああああああああああああああああああああああああ",
                Commands = 
                {
                    new InAppNotificationCommand() { Label = "コマンドテスト１" },
                    new InAppNotificationCommand() { Label = "コマンドテスト２" },
                }                
            });        
        }
    }
}
