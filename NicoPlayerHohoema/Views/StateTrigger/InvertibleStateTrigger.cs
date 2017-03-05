using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace NicoPlayerHohoema.Views.StateTrigger
{
    abstract public class InvertibleStateTrigger : StateTriggerBase
    {
        public bool Inverted { get; set; } = false;

        protected void SetActiveInvertible(bool isActive)
        {
            SetActive(Inverted ? !isActive : isActive);
        }
    }
}
