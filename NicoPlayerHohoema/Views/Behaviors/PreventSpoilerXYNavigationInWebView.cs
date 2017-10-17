using Microsoft.Xaml.Interactivity;
using NicoPlayerHohoema.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Gaming.Input;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace NicoPlayerHohoema.Views.Behaviors
{
    public class PreventSpoilerXYNavigationInWebView : Behavior<WebView>
    {
        CoreDispatcher _UIDispatcher;
        protected override void OnAttached()
        {
            AssociatedObject.GotFocus += AssociatedObject_GotFocus;
            AssociatedObject.LostFocus += AssociatedObject_LostFocus;

            _UIDispatcher = AssociatedObject.Dispatcher;
            base.OnAttached();
        }


        protected override void OnDetaching()
        {
            UINavigationManager.Pressed -= Instance_Pressed;
            base.OnDetaching(); 
        }

        private void AssociatedObject_GotFocus(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            UINavigationManager.Pressed += Instance_Pressed;
        }

        private void AssociatedObject_LostFocus(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            UINavigationManager.Pressed -= Instance_Pressed;
        }

        private async void Instance_Pressed(UINavigationManager sender, UINavigationButtons button)
        {
            await _UIDispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
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
            });
        }

    }
}
