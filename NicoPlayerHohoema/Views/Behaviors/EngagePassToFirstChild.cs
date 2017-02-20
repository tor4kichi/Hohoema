using Microsoft.Xaml.Interactivity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace NicoPlayerHohoema.Views.Behaviors
{
    public class EngagePassToFirstChild : Behavior<FrameworkElement>
    {
        private static bool _IsFirstFocus = true;

        protected override void OnAttached()
        {
            base.OnAttached();

            this.AssociatedObject.Loaded += AssociatedObject_Loaded;
            this.AssociatedObject.Unloaded += AssociatedObject_Unloaded;
        }

        private void AssociatedObject_Loaded(object sender, RoutedEventArgs e)
        {
            
            this.AssociatedObject.GotFocus += AssociatedObject_GotFocus;
            this.AssociatedObject.LostFocus += AssociatedObject_LostFocus;
        }

        private void AssociatedObject_Unloaded(object sender, RoutedEventArgs e)
        {
            this.AssociatedObject.GotFocus -= AssociatedObject_GotFocus;
            this.AssociatedObject.LostFocus -= AssociatedObject_LostFocus;
        }


        private void AssociatedObject_LostFocus(object sender, RoutedEventArgs e)
        {
            _IsFirstFocus = true;
        }

        private void AssociatedObject_GotFocus(object sender, RoutedEventArgs e)
        {
            var fe = sender as FrameworkElement;
            if (this.AssociatedObject is ItemsControl)
            {
                var itemsControl = this.AssociatedObject as ItemsControl;
                foreach (var item in itemsControl.Items)
                {
                    var control = itemsControl.ContainerFromItem(item) as FrameworkElement;
                    if (control != null)
                    {
                        var firstChildControl = control.FindFirstChild<Control>();
                        if (firstChildControl != null)
                        {
                            firstChildControl.Focus(FocusState.Programmatic);
                            break;
                        }
                    }
                }
            }
            else if (this.AssociatedObject is Panel)
            {
                var firstChildControl = this.AssociatedObject.FindFirstChild<Control>();
                if (firstChildControl != null)
                {
                    firstChildControl.Focus(FocusState.Programmatic);
                }
            }
            else
            {
                // ?
            }
        }

        


    }
}
