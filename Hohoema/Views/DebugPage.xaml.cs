using CommunityToolkit.Mvvm.DependencyInjection;
using Hohoema.Models.Notification;
using Hohoema.Services;
using Hohoema.ViewModels.Pages.Hohoema;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace Hohoema.Views.Pages.Hohoema;

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
        throw new Infra.HohoemaException("例外テスト");
    }

    private void TestCrashReport_Click(object sender, RoutedEventArgs e)
    {
        
    }

    private void TestInAppNotification(object sender, RoutedEventArgs e)
    {
        var notificationService = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<NotificationService>();
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
