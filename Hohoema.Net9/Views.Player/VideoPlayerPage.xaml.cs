#nullable enable
using CommunityToolkit.Mvvm.DependencyInjection;
using Hohoema.ViewModels.Player;
using Hohoema.ViewModels.PrimaryWindowCoreLayout;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Hohoema.Views.Player;

public sealed partial class VideoPlayerPage : Page, IDraggableAreaAware
{
    public VideoPlayerPage()
    {
        this.InitializeComponent();

        DataContext = _vm = Ioc.Default.GetRequiredService<VideoPlayerPageViewModel>();
    }

    private readonly VideoPlayerPageViewModel _vm;

    public UIElement? GetDraggableArea()
    {
        return (PlayerControlUISwitchPresenter.Content as IDraggableAreaAware)?.GetDraggableArea();
    }
}
