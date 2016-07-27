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
			
			if (TargetObject != null)
			{
				TargetObject.Visibility = Visibility.Visible;
				TargetObject.Focus(FocusState.Programmatic);
			}
			return true;
		}
	}
}
