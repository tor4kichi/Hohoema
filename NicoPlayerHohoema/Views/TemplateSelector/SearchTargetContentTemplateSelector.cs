using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace NicoPlayerHohoema.Views.TemplateSelector
{
    public class SearchTargetContentTemplateSelector : DataTemplateSelector
    {
        public DataTemplate Video { get; set; }
        public DataTemplate Mylist { get; set; }
        public DataTemplate Community { get; set; }
        public DataTemplate LiveVideo { get; set; }

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            if (item is ViewModels.VideoSearchOptionViewModelBase)
            {
                return Video;
            }
            else if (item is ViewModels.MylistSearchOptionViewModel)
            {
                return Mylist;
            }
            else if (item is ViewModels.CommunitySearchOptionViewModel)
            {
                return Community;
            }
            else if (item is ViewModels.LiveSearchOptionViewModel)
            {
                return LiveVideo;
            }

            return base.SelectTemplateCore(item, container);
        }
    }
}
