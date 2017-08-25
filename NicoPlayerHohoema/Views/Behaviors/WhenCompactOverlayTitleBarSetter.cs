using Microsoft.Xaml.Interactivity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Foundation.Metadata;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;

namespace NicoPlayerHohoema.Views.Behaviors
{
    public class WhenCompactOverlayTitleBarSetter : Behavior<UIElement>
    {
        protected override void OnAttached()
        {
            base.OnAttached();

            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 4))
            {
                Window.Current.SizeChanged += Current_SizeChanged;
            }
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();

            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 4))
            {
                Window.Current.SizeChanged -= Current_SizeChanged;
            }
        }

        private void Current_SizeChanged(object sender, Windows.UI.Core.WindowSizeChangedEventArgs e)
        {
            var view = ApplicationView.GetForCurrentView();

            if (view.ViewMode == ApplicationViewMode.CompactOverlay)
            {
                Window.Current.SetTitleBar(AssociatedObject);
            }
            else
            {
                Window.Current.SetTitleBar(null);
            }
        }
    }
}
