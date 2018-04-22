using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace NicoPlayerHohoema.Views.TemplateSelector
{
    public sealed class MenuSubPageTemplateSelector : DataTemplateSelector
    {

        public DataTemplate Video { get; set; }
        public DataTemplate Live { get; set; }


        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            try
            {
                switch (item)
                {
                    case ViewModels.VideoMenuSubPageContent _:
                        return Video;
                    case ViewModels.LiveMenuSubPageContent _:
                        return Live;
                }

            }
            catch { }

            return base.SelectTemplateCore(item, container);
        }
    }
}
