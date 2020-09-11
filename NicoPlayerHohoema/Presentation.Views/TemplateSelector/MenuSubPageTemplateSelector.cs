using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Hohoema.Presentation.Views.TemplateSelector
{
    public sealed class MenuSubPageTemplateSelector : DataTemplateSelector
    {

        public Windows.UI.Xaml.DataTemplate Video { get; set; }
        public Windows.UI.Xaml.DataTemplate Live { get; set; }
        public Windows.UI.Xaml.DataTemplate Empty { get; set; }

        protected override Windows.UI.Xaml.DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            try
            {
                switch (item)
                {
                    case ViewModels.PrimaryWindowCoreLayout.VideoMenuSubPageContent _:
                        return Video;
                    case ViewModels.PrimaryWindowCoreLayout.LiveMenuSubPageContent _:
                        return Live;
                    default:
                        return Empty;
                }

            }
            catch { }

            return base.SelectTemplateCore(item, container);
        }
    }
}
