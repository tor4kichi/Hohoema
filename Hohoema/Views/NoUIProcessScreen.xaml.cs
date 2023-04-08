using Hohoema.Services;
using Windows.UI.Xaml.Controls;

// ユーザー コントロールの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234236 を参照してください

namespace Hohoema.Views;

public sealed partial class NoUIProcessScreen : UserControl
{
    NoUIProcessScreenContext _context;
    public NoUIProcessScreen()
    {
        DataContext = _context = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<NoUIProcessScreenContext>();
        this.InitializeComponent();
    }
}
