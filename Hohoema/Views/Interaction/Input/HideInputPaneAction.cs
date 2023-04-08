using Microsoft.Xaml.Interactivity;
using Windows.UI.Xaml;

namespace Hohoema.Views.Behaviors;

public class HideInputPaneAction : DependencyObject, IAction
	{
		public object Execute(object sender, object parameter)
		{
			return Windows.UI.ViewManagement.InputPane.GetForCurrentView().TryHide();
		}
	}
