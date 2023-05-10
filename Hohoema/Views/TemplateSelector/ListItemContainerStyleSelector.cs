using Hohoema.Views.Pages.Niconico.Live;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Hohoema.Views.TemplateSelector;
public sealed class ListItemContainerStyleSelector : StyleSelector
{
    public Style VideoStyle { get; set; }
    public Style LiveStyle { get; set; }

    protected override Style SelectStyleCore(object item, DependencyObject container)
    {
        if (item is ViewModels.VideoListPage.VideoItemViewModel)
        {
            return VideoStyle;
        }
        else if (item is LiveVideoListItem)
        {
            return LiveStyle;
        }
        else
        {
            return base.SelectStyleCore(item, container);
        }
    }
}
