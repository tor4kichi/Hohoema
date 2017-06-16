using Microsoft.Xaml.Interactivity;
using NicoPlayerHohoema.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Gaming.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace NicoPlayerHohoema.Views.Behaviors
{
    public class PreventSpoilerXYNavigationInWebView : Behavior<WebView>
    {

        protected override void OnAttached()
        {            
            AssociatedObject.Loaded += AssociatedObject_Loaded;
            AssociatedObject.Unloaded += AssociatedObject_Unloaded;
            base.OnAttached();
        }

        private void AssociatedObject_Loaded(object sender, RoutedEventArgs e)
        {
            AssociatedObject.GotFocus += AssociatedObject_GotFocus;
            AssociatedObject.LostFocus += AssociatedObject_LostFocus;
        }

        private void AssociatedObject_Unloaded(object sender, RoutedEventArgs e)
        {
            AssociatedObject.GotFocus -= AssociatedObject_GotFocus;
            AssociatedObject.LostFocus -= AssociatedObject_LostFocus;

            UINavigationManager.Pressed -= Instance_Pressed;
        }

        private void AssociatedObject_GotFocus(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            UINavigationManager.Pressed += Instance_Pressed;
        }

        private void AssociatedObject_LostFocus(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            UINavigationManager.Pressed -= Instance_Pressed;
        }

        private void Instance_Pressed(UINavigationManager sender, UINavigationButtons button)
        {
            if (FocusManager.GetFocusedElement() != AssociatedObject)
            {
                return;
            }

            if (button.HasFlag(UINavigationButtons.Up))
            {
                FocusManager.TryMoveFocus(FocusNavigationDirection.Up);
            }
            else if (button.HasFlag(UINavigationButtons.Down))
            {
                FocusManager.TryMoveFocus(FocusNavigationDirection.Down);
            }
            else if (button.HasFlag(UINavigationButtons.Right))
            {
                FocusManager.TryMoveFocus(FocusNavigationDirection.Right);
            }
            else if (button.HasFlag(UINavigationButtons.Left))
            {
                FocusManager.TryMoveFocus(FocusNavigationDirection.Left);
            }
        }

    }
}
