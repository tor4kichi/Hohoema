using Microsoft.Xaml.Interactivity;
using System;
using System.Threading;
using Windows.UI.Core;
using Windows.UI.Xaml;

namespace NicoPlayerHohoema.Views.Behaviors
{
	public class AutoHide : Behavior<FrameworkElement>
	{


		#region IsEnable Property

		public static readonly DependencyProperty IsEnableProperty =
			DependencyProperty.Register("IsEnable"
					, typeof(bool)
					, typeof(AutoHide)
					, new PropertyMetadata(default(bool), OnIsEnablePropertyChanged)
				);

		public bool IsEnable
		{
			get { return (bool)GetValue(IsEnableProperty); }
			set { SetValue(IsEnableProperty, value); }
		}


		public static void OnIsEnablePropertyChanged(object sender, DependencyPropertyChangedEventArgs args)
		{
			AutoHide source = (AutoHide)sender;

			var isActive = (bool)args.NewValue;

			if (isActive)
			{
				source.EnableAutoHide();
			}
			else
			{
				source.DisableteAutoHide();
			}
		}

		#endregion



		


		#region Delay Property

		public static readonly DependencyProperty DelayProperty =
			DependencyProperty.Register("Delay"
					, typeof(TimeSpan)
					, typeof(AutoHide)
					, new PropertyMetadata(default(double))
				);

		public TimeSpan Delay
		{
			get { return (TimeSpan)GetValue(DelayProperty); }
			set { SetValue(DelayProperty, value); }
		}


		public static void OnDelayPropertyChanged(object sender, DependencyPropertyChangedEventArgs args)
		{
			AutoHide source = (AutoHide)sender;

			source._NextHideTime = source._PrevPreventTime + source.Delay;
		}

		#endregion





		#region WithCursor Property 

		public static readonly DependencyProperty WithCursorProperty =
			DependencyProperty.Register("WithCursor"
				, typeof(bool)
				, typeof(AutoHide)
				, new PropertyMetadata(true)
			);

		public bool WithCursor
		{
			get { return (bool)GetValue(WithCursorProperty); }
			set { SetValue(WithCursorProperty, value); }
		}

		#endregion







		public void PreventAutoHide()
		{
			_PrevPreventTime = DateTime.Now;
			_NextHideTime = _PrevPreventTime + Delay;

			this.AssociatedObject.Visibility = Visibility.Visible;

			CoreWindow.GetForCurrentThread().PointerCursor = _CoreCursor;
		}






		protected override void OnAttached()
		{
			base.OnAttached();

			_CoreCursor = CoreWindow.GetForCurrentThread().PointerCursor;
		}

		protected override void OnDetaching()
		{
			base.OnDetaching();

			_Timer?.Dispose();
			_Timer = null;

			CoreWindow.GetForCurrentThread().PointerCursor = _CoreCursor;
		}




		private void EnableAutoHide()
		{
			_NextHideTime = DateTime.Now + Delay;

			tokenSource = new CancellationTokenSource();
			
			_Timer = new Timer(async (state) => 
			{
				await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
				{
					if (this.AssociatedObject.Visibility == Visibility.Visible &&
						_NextHideTime < DateTime.Now)
					{
						this.AssociatedObject.Visibility = Visibility.Collapsed;

						if (WithCursor)
						{
							CoreWindow.GetForCurrentThread().PointerCursor = null;
						}
					}
				});
			}
			, this, Delay, TimeSpan.FromMilliseconds(25));
			


		}

		private void DisableteAutoHide()
		{
			_Timer?.Dispose();
			_Timer = null;

			this.AssociatedObject.Visibility = Visibility.Visible;
			CoreWindow.GetForCurrentThread().PointerCursor = _CoreCursor;
			
		}


		CancellationTokenSource tokenSource;

		CoreCursor _CoreCursor;
		DateTime _PrevPreventTime;
		DateTime _NextHideTime;
		Timer _Timer;
	}
}
