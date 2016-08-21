using Microsoft.Xaml.Interactivity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace NicoPlayerHohoema.Views.Behaviors
{
	public class HideInputPaneAction : DependencyObject, IAction
	{
		public object Execute(object sender, object parameter)
		{
			return Windows.UI.ViewManagement.InputPane.GetForCurrentView().TryHide();
		}
	}
}
