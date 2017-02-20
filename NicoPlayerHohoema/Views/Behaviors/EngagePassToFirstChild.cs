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
                        var firstChildControl = FindFirstChild<Control>(control);
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
                var firstChildControl = FindFirstChild<Control>(this.AssociatedObject);
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

        static T FindFirstChild<T>(FrameworkElement element) where T : FrameworkElement
        {
            int childrenCount = VisualTreeHelper.GetChildrenCount(element);
            var children = new FrameworkElement[childrenCount];

            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(element, i) as FrameworkElement;
                children[i] = child;
                if (child is T)
                    return (T)child;
            }

            for (int i = 0; i < childrenCount; i++)
                if (children[i] != null)
                {
                    var subChild = FindFirstChild<T>(children[i]);
                    if (subChild != null)
                        return subChild;
                }

            return null;
        }


    }
}
