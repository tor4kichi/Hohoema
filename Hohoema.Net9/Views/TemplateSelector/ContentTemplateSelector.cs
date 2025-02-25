#nullable enable
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Hohoema.Views.TemplateSelector;

public sealed partial class ContentTemplateSelector : DataTemplateSelector
{
    public Windows.UI.Xaml.DataTemplate ContentTemplate { get; set; }
    public Windows.UI.Xaml.DataTemplate DefaultTemplate { get; set; }

    protected override Windows.UI.Xaml.DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        if (item != null && ContentTemplate != null)
        {
            return ContentTemplate;
        }
        else if (DefaultTemplate != null)
        {
            return DefaultTemplate;
        }

        return base.SelectTemplateCore(item, container);
    }
}
