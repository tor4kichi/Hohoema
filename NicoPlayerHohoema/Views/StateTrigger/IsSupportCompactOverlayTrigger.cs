using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation.Metadata;
using Windows.UI.ViewManagement;

namespace NicoPlayerHohoema.Views.StateTrigger
{
    public class IsSupportCompactOverlayTrigger : InvertibleStateTrigger
    {
        public IsSupportCompactOverlayTrigger()
        {
            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 4))
            {
                var view = ApplicationView.GetForCurrentView();
                SetActiveInvertible(view.IsViewModeSupported(ApplicationViewMode.CompactOverlay));
            }
            else
            {
                SetActiveInvertible(false);
            }
        }
    }
}
