#nullable enable
using CommunityToolkit.Mvvm.DependencyInjection;
using Hohoema.ViewModels.Pages.Niconico.Community;
using Windows.UI.Xaml.Controls;

// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace Hohoema.Views.Pages.Niconico.Community;

/// <summary>
/// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
/// </summary>
public sealed partial class CommunityPage : Page
{
    public CommunityPage()
    {
        this.InitializeComponent();
        DataContext = _vm = Ioc.Default.GetRequiredService<CommunityPageViewModel>();
    }

    private readonly CommunityPageViewModel _vm;
}
