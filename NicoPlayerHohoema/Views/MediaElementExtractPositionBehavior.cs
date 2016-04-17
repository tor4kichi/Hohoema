using Microsoft.Xaml.Interactivity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace NicoPlayerHohoema.Views
{
	public class MediaElementExtractPositionBehavior : Behavior<MediaElement>
	{

		public MediaElementExtractPositionBehavior()
		{
			Position = new TimeSpan();
			Interval = new TimeSpan();
		}

		private Timer _ExtractTimingTimer;


		public static readonly DependencyProperty PositionProperty =
			DependencyProperty.Register("Position"
					, typeof(TimeSpan)
					, typeof(MediaElementExtractPositionBehavior)
					, new PropertyMetadata(default(TimeSpan))
				);

		public TimeSpan Position
		{
			get { return (TimeSpan)GetValue(PositionProperty); }
			set { SetValue(PositionProperty, value); }
		}




		public static readonly DependencyProperty IntervalProperty =
			DependencyProperty.Register("Interval"
					, typeof(TimeSpan)
					, typeof(MediaElementExtractPositionBehavior)
					, new PropertyMetadata(default(TimeSpan))
				);

		public TimeSpan Interval
		{
			get { return (TimeSpan)GetValue(IntervalProperty); }
			set { SetValue(IntervalProperty, value); }
		}


		protected override void OnAttached()
		{
			base.OnAttached();

			this.AssociatedObject.Loaded += AssociatedObject_Loaded;
			
		}


		private void AssociatedObject_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
		{
			UpdateTimerSetting();


			this.AssociatedObject.Loaded -= AssociatedObject_Loaded;
			this.AssociatedObject.Unloaded += AssociatedObject_Unloaded;
		}

		private void AssociatedObject_Unloaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
		{
			_ExtractTimingTimer?.Dispose();
			_ExtractTimingTimer = null;
		}


		protected override void OnDetaching()
		{
			base.OnDetaching();

		}






		private void UpdateTimerSetting()
		{
			_ExtractTimingTimer?.Dispose();
			_ExtractTimingTimer = null;


			_ExtractTimingTimer = new Timer(async (x) =>
			{
				var me = (MediaElementExtractPositionBehavior)x;
				var mediaElem = me.AssociatedObject as MediaElement;

				await me.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low,
					() => me.Position = mediaElem.Position);


			}, this, 0, (int)Interval.TotalMilliseconds);
		}

	}
}
