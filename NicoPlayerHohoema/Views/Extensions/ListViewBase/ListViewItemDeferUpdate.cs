using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace NicoPlayerHohoema.Views.Extensions
{
    public partial class ListViewBase
    {
        public static readonly DependencyProperty DeferUpdateProperty =
           DependencyProperty.RegisterAttached(
               "DeferUpdate",
               typeof(bool),
               typeof(ListViewBase),
               new PropertyMetadata(false, DeferUpdatePropertyChanged)
           );

        public static void SetDeferUpdate(UIElement element, bool value)
        {
            element.SetValue(DeferUpdateProperty, value);
        }
        public static bool GetDeferUpdate(UIElement element)
        {
            return (bool)element.GetValue(DeferUpdateProperty);
        }

        private static void DeferUpdatePropertyChanged(DependencyObject s, DependencyPropertyChangedEventArgs e)
        {
            if (s is Windows.UI.Xaml.Controls.ListViewBase target)
            {
                var enabled = (bool)e.NewValue;
                if (enabled)
                {
                    target.ContainerContentChanging += (sender, args) =>
                    {
                        if (args.InRecycleQueue) { return; }

                        if (args.Item is Interfaces.IListViewBaseItemDeferUpdatable updatable)
                        {
                            updatable.DeferUpdate();
                        }
                    };
                }
            }
        }
    }
}
