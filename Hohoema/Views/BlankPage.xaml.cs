using CommunityToolkit.Mvvm.DependencyInjection;
using Hohoema.ViewModels.Pages;
using Windows.UI.Xaml.Controls;

// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace Hohoema.Views.Pages;

/// <summary>
/// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
/// </summary>
public sealed partial class BlankPage : Page
{
    public BlankPage()
    {
        this.InitializeComponent();

        DataContext = Ioc.Default.GetRequiredService<BlankPageViewModel>();
    }
}
