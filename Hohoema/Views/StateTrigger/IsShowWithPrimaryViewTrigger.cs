using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;

namespace Hohoema.Views.StateTrigger
{
    public sealed class IsShowWithPrimaryViewTrigger : InvertibleStateTrigger
    {
        public IsShowWithPrimaryViewTrigger()
        {
            var coreApplication = CoreApplication.GetCurrentView();
            SetActiveInvertible(coreApplication.IsMain);
        }
    }
}
