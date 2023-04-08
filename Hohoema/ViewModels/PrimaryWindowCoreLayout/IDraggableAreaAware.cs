#nullable enable
using Windows.UI.Xaml;

namespace Hohoema.ViewModels.PrimaryWindowCoreLayout;

public interface IDraggableAreaAware
{
    public UIElement? GetDraggableArea();
}
