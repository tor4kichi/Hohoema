using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace Hohoema.Presentation.Views.Extensions.VIsualStateManager
{
    public partial class VisualStateManagerExtensions : DependencyObject
    {
        public static readonly DependencyProperty DebugCurrentStateProperty =
          DependencyProperty.RegisterAttached(
              "DebugCurrentState",
              typeof(bool),
              typeof(VisualStateGroup),
              new PropertyMetadata(false, DebugCurrentStatePropertyChanged)
          );

        private static void DebugCurrentStatePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
#if DEBUG
            var group = (VisualStateGroup)d;
            if (e.NewValue is bool val && val)
            {
                group.CurrentStateChanging += Group_CurrentStateChanging;
                group.CurrentStateChanged += Group_CurrentStateChanged;
            }
            else
            {
                group.CurrentStateChanging -= Group_CurrentStateChanging;
                group.CurrentStateChanged -= Group_CurrentStateChanged;
            }
#endif
        }

        private static void Group_CurrentStateChanging(object sender, VisualStateChangedEventArgs e)
        {
            Debug.WriteLine($"[VSM Debug] changing: {e.OldState?.Name ?? "-"} ---> {e.NewState?.Name ?? "-"}");
        }


        private static void Group_CurrentStateChanged(object sender, VisualStateChangedEventArgs e)
        {
            Debug.WriteLine($"[VSM Debug] changed : {e.OldState?.Name ?? "-"} ---> {e.NewState?.Name ?? "-"}");
        }


        public static void SetDebugCurrentState(DependencyObject element, bool value)
        {
            element.SetValue(DebugCurrentStateProperty, value);
        }
        public static bool GetDebugCurrentState(DependencyObject element)
        {
            return (bool)element.GetValue(DebugCurrentStateProperty);
        }
    }
}
