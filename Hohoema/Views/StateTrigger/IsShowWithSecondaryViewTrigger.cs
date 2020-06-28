using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.UI.Xaml;

namespace Hohoema.Views.StateTrigger
{
    public sealed class IsShowWithSecondaryViewTrigger : StateTriggerBase
    {
        public IsShowWithSecondaryViewTrigger()
        {
            var coreApplication = CoreApplication.GetCurrentView();
            SetActive(!coreApplication.IsMain);
        }
    }
}
