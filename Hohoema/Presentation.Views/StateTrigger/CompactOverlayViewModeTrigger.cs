using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation.Metadata;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;

namespace Hohoema.Presentation.Views.StateTrigger
{
    public sealed class CompactOverlayViewModeTrigger : InvertibleStateTrigger, IDisposable
    {
        public CompactOverlayViewModeTrigger()
        {
            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 4))
            {
                Update();
                Window.Current.SizeChanged += Current_SizeChanged;
            }
            else
            {
                SetActiveInvertible(false);
            }
        }

        public void Dispose()
        {
            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 4))
            {
                Window.Current.SizeChanged -= Current_SizeChanged;
            }
        }

        private void Current_SizeChanged(object sender, Windows.UI.Core.WindowSizeChangedEventArgs e)
        {
            Update();
        }

        private void Update()
        {
            var view = ApplicationView.GetForCurrentView();

            SetActiveInvertible(view.ViewMode == ApplicationViewMode.CompactOverlay);
        }
    }
}
