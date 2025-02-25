#nullable enable
using Hohoema.Models.Niconico.Video;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Hohoema.Views.Player.VideoPlayerUI;

public sealed partial class VideoQualityListItemContainerStyleSelector : StyleSelector
{
    public Style AvairableQuality { get; set; }
    public Style UnavairableQuality { get; set; }

    protected override Style SelectStyleCore(object item, DependencyObject container)
    {
        if (item is NicoVideoQualityEntity quality)
        {
            return quality.IsAvailable
                ? AvairableQuality
                : UnavairableQuality
                ;
        }

        return base.SelectStyleCore(item, container);
    }
}
