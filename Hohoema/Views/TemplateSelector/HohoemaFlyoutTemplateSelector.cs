﻿using Hohoema.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Hohoema.Views.TemplateSelector
{
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

        protected override Windows.UI.Xaml.DataTemplate SelectTemplateCore(object item)
        {
            if (item is ViewModels.VideoInfoControlViewModel)
            {
                return VideoFlyoutTemplate;
            }

            return base.SelectTemplateCore(item);
        }
    }
}
