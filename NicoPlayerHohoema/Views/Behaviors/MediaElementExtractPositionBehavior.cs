using Microsoft.Xaml.Interactivity;
using System;
using System.Threading;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;


namespace NicoPlayerHohoema.Views.Behaviors
{
	public class MediaElementExtractPositionBehavior : Behavior<MediaElement>
	{		



		#region Position Property

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

		#endregion


		#region Interval Property

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

		#endregion


		#region MediaElement Event Handling

		protected override void OnAttached()
		{
			base.OnAttached();

			this.AssociatedObject.Loaded += AssociatedObject_Loaded;
			
		}


		private void AssociatedObject_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
		{
			this.AssociatedObject.CurrentStateChanged += AssociatedObject_CurrentStateChanged;
			this.AssociatedObject.SeekCompleted += AssociatedObject_SeekCompleted;


			this.AssociatedObject.Loaded -= AssociatedObject_Loaded;
			this.AssociatedObject.Unloaded += AssociatedObject_Unloaded;
		}

		private void AssociatedObject_SeekCompleted(object sender, RoutedEventArgs e)
		{
			this.ExtractPosition();
		}

		private void AssociatedObject_CurrentStateChanged(object sender, RoutedEventArgs e)
		{
			var mediaElem = (MediaElement)sender;

			switch (mediaElem.CurrentState)
			{
				case Windows.UI.Xaml.Media.MediaElementState.Closed:
					TimerExit();
					break;
				case Windows.UI.Xaml.Media.MediaElementState.Opening:
					RefreshTimerSetting();
					break;
				case Windows.UI.Xaml.Media.MediaElementState.Buffering:
					break;
				case Windows.UI.Xaml.Media.MediaElementState.Playing:
					RefreshTimerSetting();
					break;
				case Windows.UI.Xaml.Media.MediaElementState.Paused:
					TimerExit();
					break;
				case Windows.UI.Xaml.Media.MediaElementState.Stopped:
					TimerExit();
					break;
				default:
					break;
			}
		}

		private void AssociatedObject_Unloaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
		{
			TimerExit();
		}


		protected override void OnDetaching()
		{
			base.OnDetaching();

			this.AssociatedObject.CurrentStateChanged -= AssociatedObject_CurrentStateChanged;
			this.AssociatedObject.SeekCompleted -= AssociatedObject_SeekCompleted;


			TimerExit();
		}


		#endregion


		#region Management Timer


		private Timer _ExtractTimingTimer;


		private void RefreshTimerSetting()
		{
			TimerExit();

			_ExtractTimingTimer = new Timer((x) =>
			{
				var me = (MediaElementExtractPositionBehavior)x;

				me.ExtractPosition();

			}, this, 0, (int)Interval.TotalMilliseconds);
		}

		private void TimerExit()
		{
			_ExtractTimingTimer?.Dispose();
			_ExtractTimingTimer = null;
		}


		private async void ExtractPosition()
		{
			var mediaElem = this.AssociatedObject as MediaElement;
			
			await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low,
					() => this.Position = mediaElem.Position);
		}


		#endregion
	}
}
