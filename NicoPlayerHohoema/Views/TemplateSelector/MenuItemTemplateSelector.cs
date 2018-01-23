using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace NicoPlayerHohoema.Views.TemplateSelector
{
    public sealed class MenuItemTemplateSelector : DataTemplateSelector
    {
        public DataTemplate SubMenuItem { get; set; }
        public DataTemplate MenuItem { get; set; }

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            if (item is ViewModels.MenuSubItemViewModel)
            {
                return SubMenuItem;
            }
            else if (item is ViewModels.MenuItemViewModel)
            {
                return MenuItem;
            }

            return base.SelectTemplateCore(item, container);
        }
    }
}
