using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace Hohoema.Presentation.Views.StateTrigger
{
    
    abstract public class InvertibleStateTrigger : StateTriggerBase, ITriggerValue
    {
        public bool Inverted { get; set; } = false;

        public bool IsActive
        {
            get { return (bool)GetValue(IsActiveProperty); }
            private set { SetValue(IsActiveProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsActive.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsActiveProperty =
            DependencyProperty.Register("IsActive", typeof(bool), typeof(InvertibleStateTrigger), new PropertyMetadata(true));

        public event EventHandler IsActiveChanged;

        protected void SetActiveInvertible(bool isActive)
        {
            var oldVal = IsActive;

            var valInvertApplied = Inverted ? !isActive : isActive;
            if (oldVal == valInvertApplied) { return; }

            IsActive = valInvertApplied;
            SetActive(valInvertApplied);
            IsActiveChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
