using Microsoft.Xaml.Interactivity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace NicoPlayerHohoema.Views.Behaviors
{
	class SetFocus : Behavior<DependencyObject>, IAction
	{
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


		public object Execute(object sender, object parameter)
		{
            if (TargetObject == null) { return false; }

            if (Delay != TimeSpan.Zero)
            {
                
                Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () => 
                {
                    await Task.Delay(Delay);

                    TargetObject.Visibility = Visibility.Visible;
                    TargetObject.Focus(FocusState.Programmatic);
                })
                .AsTask().ConfigureAwait(false);
            }
            else
            {
                if (TargetObject != null)
                {
                    TargetObject.Visibility = Visibility.Visible;
                    TargetObject.Focus(FocusState.Programmatic);
                }
            }

            return true;
		}
	}
}
