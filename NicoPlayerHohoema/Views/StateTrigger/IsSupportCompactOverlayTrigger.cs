using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.ViewManagement;

namespace NicoPlayerHohoema.Views.StateTrigger
{
    public class IsSupportCompactOverlayTrigger : InvertibleStateTrigger
    {
        public IsSupportCompactOverlayTrigger()
        {
            var view = ApplicationView.GetForCurrentView();
            SetActiveInvertible(view.IsViewModeSupported(ApplicationViewMode.CompactOverlay));
        }
    }
}
