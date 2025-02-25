#nullable enable
using CommunityToolkit.Mvvm.DependencyInjection;
using Hohoema.ViewModels.Niconico.Video.Commands;
using Windows.UI.Xaml.Controls;

namespace Hohoema.Views.Flyouts;

public sealed partial class MylistItemFlyout : MenuFlyout
{
    public MylistItemFlyout()
    {
        this.InitializeComponent();

        PlayAllItem.Command = Ioc.Default.GetService<PlaylistPlayAllCommand>();
    }
}
