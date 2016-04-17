using Microsoft.Xaml.Interactivity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace NicoPlayerHohoema.Views.Behaviors
{
	public class MediaElementStateGetter : Behavior<MediaElement>
	{
		#region Position Property

		public static readonly DependencyProperty StateProperty =
			DependencyProperty.Register("State"
					, typeof(MediaElementState)
					, typeof(MediaElementStateGetter)
					, new PropertyMetadata(default(MediaElementState))
				);

		public MediaElementState State
		{
			get { return (MediaElementState)GetValue(StateProperty); }
			set { SetValue(StateProperty, value); }
		}

		#endregion



		protected override void OnAttached()
		{
			base.OnAttached();

			this.AssociatedObject.Loaded += AssociatedObject_Loaded;
		}

		private void AssociatedObject_Loaded(object sender, RoutedEventArgs e)
		{
			this.AssociatedObject.CurrentStateChanged += AssociatedObject_CurrentStateChanged;
		}

		private void AssociatedObject_CurrentStateChanged(object sender, RoutedEventArgs e)
		{
			State = this.AssociatedObject.CurrentState;
		}

		protected override void OnDetaching()
		{
			base.OnDetaching();

			this.AssociatedObject.CurrentStateChanged -= AssociatedObject_CurrentStateChanged;
		}
	}
}
