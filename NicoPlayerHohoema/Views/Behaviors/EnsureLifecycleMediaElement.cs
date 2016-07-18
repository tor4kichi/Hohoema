using Microsoft.Xaml.Interactivity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace NicoPlayerHohoema.Views.Behaviors
{
	public class EnsureLifecycleMediaElement : Behavior<MediaElement>
	{
		protected override void OnAttached()
		{
			base.OnAttached();

			this.AssociatedObject.Loaded += AssociatedObject_Loaded;
		}

		private void AssociatedObject_Loaded(object sender, RoutedEventArgs e)
		{
			ApplicationView.GetForCurrentView().Consolidated += EnsureLifecycleMediaElement_Consolidated;
			
		}

		private void EnsureLifecycleMediaElement_Consolidated(ApplicationView sender, ApplicationViewConsolidatedEventArgs args)
		{
			this.AssociatedObject.Stop();
		}

		protected override void OnDetaching()
		{
			base.OnDetaching();

		}
	}
}
