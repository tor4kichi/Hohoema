using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;

namespace NicoPlayerHohoema.Views.StateTrigger
{
    public class CompactOverlayViewModeTrigger : InvertibleStateTrigger
    {
        public CompactOverlayViewModeTrigger()
        {
            Update();
            Window.Current.SizeChanged += Current_SizeChanged;
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
