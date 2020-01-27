using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using WindowsStateTriggers;

namespace NicoPlayerHohoema.Views.StateTrigger
{
    abstract public class InvertibleStateTrigger : StateTriggerBase, ITriggerValue
    {
        public bool Inverted { get; set; } = false;

        bool _isActive;
        public bool IsActive 
        {
            get => _isActive;
            set
            {
                if (_isActive != value)
                {
                    _isActive = value;
                    IsActiveChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public event EventHandler IsActiveChanged;

        protected void SetActiveInvertible(bool isActive)
        {
            SetActive(IsActive = Inverted ? !isActive : isActive);
        }
    }
}
