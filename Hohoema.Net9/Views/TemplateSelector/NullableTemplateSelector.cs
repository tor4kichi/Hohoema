#nullable enable
using Hohoema.Models.Niconico.Video;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Hohoema.Views.TemplateSelector;

public sealed partial class NicoVideoCacheQualityTemplateSelector : DataTemplateSelector
{
    public Windows.UI.Xaml.DataTemplate UnknownTemplate { get; set; }
    public Windows.UI.Xaml.DataTemplate DefaultTemplate { get; set; }


    protected override Windows.UI.Xaml.DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        if (item is NicoVideoQuality quality
            && quality == NicoVideoQuality.Unknown 
            && UnknownTemplate != null
            )
        {
            return UnknownTemplate;
        }
        else if (DefaultTemplate != null)
        {
            return DefaultTemplate;
        }

        return base.SelectTemplateCore(item, container);
    }
}
