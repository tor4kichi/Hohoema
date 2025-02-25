#nullable enable
using CommunityToolkit.Mvvm.DependencyInjection;
using Hohoema.ViewModels.Pages.Hohoema.Queue;
using Windows.UI.Xaml.Controls;

// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace Hohoema.Views.Pages.Hohoema.Queue;

/// <summary>
/// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
/// </summary>
public sealed partial class VideoQueuePage : Page
{
    public VideoQueuePage()
    {
        this.InitializeComponent();
        DataContext = _vm = Ioc.Default.GetRequiredService<VideoQueuePageViewModel>();
    }

    private readonly VideoQueuePageViewModel _vm;
}
