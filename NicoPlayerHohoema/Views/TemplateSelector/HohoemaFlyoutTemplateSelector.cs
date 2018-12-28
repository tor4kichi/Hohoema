using NicoPlayerHohoema.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace NicoPlayerHohoema.Views.TemplateSelector
{
    public class HohoemaFlyoutTemplateSelector : DataTemplateSelector
    {
        public DataTemplate VideoFlyoutTemplate { get; set; }
        public DataTemplate LiveFlyoutTemplate { get; set; }
        public DataTemplate MylistFlyoutTemplate { get; set; }
        public DataTemplate UserFlyoutTemplate { get; set; }
        public DataTemplate CommunityFlyoutTemplate { get; set; }
        public DataTemplate SearchHistoryFlyoutTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            if (item is ViewModels.VideoInfoControlViewModel)
            {
                return VideoFlyoutTemplate;
            }
            else if (item is ILiveContent)
            {
                return LiveFlyoutTemplate;
            }
            else if (item is ViewModels.SearchHistoryListItem)
            {
                return SearchHistoryFlyoutTemplate;
            }

            return base.SelectTemplateCore(item, container);
        }

        protected override DataTemplate SelectTemplateCore(object item)
        {
            if (item is ViewModels.VideoInfoControlViewModel)
            {
                return VideoFlyoutTemplate;
            }

            return base.SelectTemplateCore(item);
        }
    }
}
