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
		public bool RecentlyPlayed { get; private set; }
		protected override void OnAttached()
		{
			base.OnAttached();

			this.AssociatedObject.Loaded += AssociatedObject_Loaded;
		}

		private void AssociatedObject_Loaded(object sender, RoutedEventArgs e)
		{
			Window.Current.VisibilityChanged += Current_VisibilityChanged;
			
		}

		private void Current_VisibilityChanged(object sender, Windows.UI.Core.VisibilityChangedEventArgs e)
		{
			if (e.Visible)
			{
				if (RecentlyPlayed)
				{
					this.AssociatedObject.Play();
					RecentlyPlayed = false;
				}
			}
			else
			{
				RecentlyPlayed = AssociatedObject.CurrentState == Windows.UI.Xaml.Media.MediaElementState.Playing
					|| AssociatedObject.CurrentState == Windows.UI.Xaml.Media.MediaElementState.Opening
					|| AssociatedObject.CurrentState == Windows.UI.Xaml.Media.MediaElementState.Buffering;

				if (this.AssociatedObject.CanPause)
				{
					this.AssociatedObject.Pause();
				}
			}
		}

		private void EnsureLifecycleMediaElement_Consolidated(ApplicationView sender, ApplicationViewConsolidatedEventArgs args)
		{
			
		}

		protected override void OnDetaching()
		{
			base.OnDetaching();

			Window.Current.VisibilityChanged -= Current_VisibilityChanged;
		}
	}
}
