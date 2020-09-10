using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Hohoema.Presentation.Views.TemplateSelector
{
    public class SearchTargetContentTemplateSelector : DataTemplateSelector
    {
        public Windows.UI.Xaml.DataTemplate Video { get; set; }
        public Windows.UI.Xaml.DataTemplate Mylist { get; set; }
        public Windows.UI.Xaml.DataTemplate Community { get; set; }
        public Windows.UI.Xaml.DataTemplate LiveVideo { get; set; }

        protected override Windows.UI.Xaml.DataTemplate SelectTemplateCore(object item, DependencyObject container)
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
            //else if (item is ViewModels.LiveSearchOptionViewModel)
            //{
            //    return LiveVideo;
            //}

            return base.SelectTemplateCore(item, container);
        }
    }
}
