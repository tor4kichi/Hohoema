#nullable enable

using Hohoema.Models.Niconico.Live;
using Hohoema.ViewModels.Pages.Niconico.Search;
using Hohoema.ViewModels.VideoListPage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Hohoema.Views.TemplateSelector;

public class HohoemaFlyoutTemplateSelector : DataTemplateSelector
{
    public Windows.UI.Xaml.DataTemplate VideoFlyoutTemplate { get; set; }
    public Windows.UI.Xaml.DataTemplate LiveFlyoutTemplate { get; set; }
    public Windows.UI.Xaml.DataTemplate MylistFlyoutTemplate { get; set; }
    public Windows.UI.Xaml.DataTemplate UserFlyoutTemplate { get; set; }
    public Windows.UI.Xaml.DataTemplate CommunityFlyoutTemplate { get; set; }
    public Windows.UI.Xaml.DataTemplate SearchHistoryFlyoutTemplate { get; set; }

    protected override Windows.UI.Xaml.DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        if (item is VideoListItemControlViewModel)
        {
            return VideoFlyoutTemplate;
        }
        else if (item is ILiveContent)
        {
            return LiveFlyoutTemplate;
        }
        else if (item is SearchHistoryListItemViewModel)
        {
            return SearchHistoryFlyoutTemplate;
        }

        return base.SelectTemplateCore(item, container);
    }

    protected override Windows.UI.Xaml.DataTemplate SelectTemplateCore(object item)
    {
        if (item is VideoListItemControlViewModel)
        {
            return VideoFlyoutTemplate;
        }

        return base.SelectTemplateCore(item);
    }
}
