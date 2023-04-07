using CommunityToolkit.Mvvm.Messaging.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace Hohoema.ViewModels.PrimaryWindowCoreLayout
{
    public interface IDraggableAreaAware
    {
        public UIElement? GetDraggableArea();
    }
}
