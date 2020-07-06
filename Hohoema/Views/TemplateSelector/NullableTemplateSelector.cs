﻿using Hohoema.Models.Niconico.Video;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Hohoema.Views.TemplateSelector
{
    public sealed class NicoVideoCacheQualityTemplateSelector : DataTemplateSelector
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
}
