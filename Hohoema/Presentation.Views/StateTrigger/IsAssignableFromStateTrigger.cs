using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using WindowsStateTriggers;

namespace Hohoema.Presentation.Views.StateTrigger
{
    public sealed class IsAssignableFromStateTrigger : StateTriggerBase, ITriggerValue
    {
        public object Value
        {
            get { return (object)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Value.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(object), typeof(IsAssignableFromStateTrigger), new PropertyMetadata(null, OnValueChanged));

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as IsAssignableFromStateTrigger).RefreshIsActive();
        }

        public Type TargetType
        {
            get { return (Type)GetValue(TargetTypeProperty); }
            set { SetValue(TargetTypeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for TargetType.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TargetTypeProperty =
            DependencyProperty.Register("TargetType", typeof(Type), typeof(IsAssignableFromStateTrigger), new PropertyMetadata(null, OnTargetTypeChanged));

        private static void OnTargetTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as IsAssignableFromStateTrigger).RefreshIsActive();
        }

        public bool IsActive { get; private set; }

        public event EventHandler IsActiveChanged;


        private void RefreshIsActive()
        {
            bool isActive = IsActive;
            var value = Value;
            var targetType = TargetType;
            if (value is null || targetType is null) 
            {
                isActive = false;
            }
            else
            {
                isActive = targetType.IsAssignableFrom(value.GetType());
            }

            if (isActive != IsActive)
            {
                SetActive(isActive);
                IsActiveChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
