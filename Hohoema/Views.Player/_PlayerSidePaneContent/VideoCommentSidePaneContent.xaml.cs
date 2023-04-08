#nullable enable
using Hohoema.ViewModels.Player.PlayerSidePaneContent;
using Windows.UI.Xaml.Controls;

// ユーザー コントロールの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234236 を参照してください

namespace Hohoema.Views.Player;

public sealed partial class VideoCommentSidePaneContent : UserControl
{
    VideoCommentSidePaneContentViewModel _viewModel;
    public VideoCommentSidePaneContent()
    {
        DataContext = _viewModel = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<VideoCommentSidePaneContentViewModel>();
        this.InitializeComponent();
    }
}
