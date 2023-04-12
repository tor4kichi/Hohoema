#nullable enable
using CommunityToolkit.Mvvm.DependencyInjection;
using Hohoema.ViewModels.Pages.Niconico.Mylist;
using Windows.UI.Xaml.Controls;

// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace Hohoema.Views.Pages.Niconico.Mylist;

/// <summary>
/// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
/// </summary>
public sealed partial class OwnerMylistManagePage : Page
{
    public OwnerMylistManagePage()
    {
        this.InitializeComponent();
        DataContext = _vm = Ioc.Default.GetRequiredService<OwnerMylistManagePageViewModel>();
    }

    private readonly OwnerMylistManagePageViewModel _vm;
}
