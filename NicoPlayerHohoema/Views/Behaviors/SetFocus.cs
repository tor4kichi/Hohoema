using Microsoft.Xaml.Interactivity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace NicoPlayerHohoema.Views.Behaviors
{
	class SetFocus : Behavior<DependencyObject>, IAction
	{
        public static readonly DependencyProperty IsEnabledProperty =
           DependencyProperty.Register("IsEnabled"
               , typeof(bool)
               , typeof(SetFocus)
               , new PropertyMetadata(true)
               );
        public bool IsEnabled
        {
            get { return (bool)GetValue(IsEnabledProperty); }
            set { SetValue(IsEnabledProperty, value); }
        }

       


        public static readonly DependencyProperty DelayProperty =
            DependencyProperty.Register("Delay"
                    , typeof(TimeSpan)
                    , typeof(SetFocus)
                    , new PropertyMetadata(TimeSpan.Zero)
                );

        public TimeSpan Delay
        {
            get { return (TimeSpan)GetValue(DelayProperty); }
            set { SetValue(DelayProperty, value); }
        }

        public static readonly DependencyProperty StateProperty =
			DependencyProperty.Register("TargetObject"
					, typeof(Control)
					, typeof(SetFocus)
					, new PropertyMetadata(null)
				);

		public Control TargetObject
		{
			get { return (Control)GetValue(StateProperty); }
			set { SetValue(StateProperty, value); }
		}



        public static readonly DependencyProperty FocusStateProperty =
           DependencyProperty.Register(nameof(FocusState)
               , typeof(FocusState)
               , typeof(SetFocus)
               , new PropertyMetadata(FocusState.Programmatic)
               );
        public FocusState FocusState
        {
            get { return (FocusState)GetValue(FocusStateProperty); }
            set { SetValue(FocusStateProperty, value); }
        }

        public object Execute(object sender, object parameter)
		{
            if (!IsEnabled) { return null; }

            var target = TargetObject;
            if (target == null)
            {
                target = AssociatedObject as Control;
            }

            if (target == null) { return false; }

            if (Delay != TimeSpan.Zero)
            {
                Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () => 
                {
                    await Task.Delay(Delay);

                    Focus();
                })
                .AsTask().ConfigureAwait(false);
            }
            else
            {
                Focus();
            }

            return true;
		}

        private bool Focus()
        {
            if (TargetObject != null)
            {
                return TargetObject.Focus(FocusState.Programmatic);
            }
            else
            {
                return FocusManager.TryMoveFocus(FocusNavigationDirection.None,
                    new FindNextElementOptions() { SearchRoot = AssociatedObject }
                    );
            }
        }
	}
}
