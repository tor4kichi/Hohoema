using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation.Metadata;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;

namespace NicoPlayerHohoema.Views.StateTrigger
{
    public class IsSupportCompactOverlayTrigger : StateTriggerBase
    {
        public IsSupportCompactOverlayTrigger()
        {
            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 4))
            {
                var view = ApplicationView.GetForCurrentView();
                var supported = view.IsViewModeSupported(ApplicationViewMode.CompactOverlay);
                SetActive(supported);
            }
            else
            {
                SetActive(false);
            }
        }
    }
}
