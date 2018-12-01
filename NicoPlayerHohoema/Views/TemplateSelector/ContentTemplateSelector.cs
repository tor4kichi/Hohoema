using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace NicoPlayerHohoema.Views.TemplateSelector
{
    public sealed class ContentTemplateSelector : DataTemplateSelector
    {
        public DataTemplate ContentTemplate { get; set; }
        public DataTemplate DefaultTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
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
}
