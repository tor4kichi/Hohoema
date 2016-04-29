using Microsoft.Xaml.Interactivity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml;
using WinRTXamlToolkit.Controls.Extensions;

namespace NicoPlayerHohoema.Views.Behaviors
{
	public class AutoHide : Behavior<FrameworkElement>
	{
		// MouseMoveとタップの検出
		// カーソルのAutoHide
		#region HostScreen Property 

		public static readonly DependencyProperty HostScreenProperty =
		DependencyProperty.Register("HostScreen"
				, typeof(FrameworkElement)
				, typeof(AutoHide)
				, new PropertyMetadata(default(FrameworkElement))
			);

		public FrameworkElement HostScreen
		{
			get { return (FrameworkElement)GetValue(HostScreenProperty); }
			set { SetValue(HostScreenProperty, value); }
		}

		#endregion

		#region WithCursol Property 
		public static readonly DependencyProperty WithCursolProperty =
		DependencyProperty.Register("WithCursol"
				, typeof(bool)
				, typeof(AutoHide)
				, new PropertyMetadata(true)
			);

		public bool WithCursol
		{
			get { return (bool)GetValue(WithCursolProperty); }
			set { SetValue(WithCursolProperty, value); }
		}

		#endregion



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




		protected override void OnAttached()
		{
			base.OnAttached();

			_CoreCursol = CoreWindow.GetForCurrentThread().PointerCursor;

		}

		protected override void OnDetaching()
		{
			base.OnDetaching();

			_Timer?.Dispose();
			_Timer = null;

			var hostScreen = HostScreen;
			if (hostScreen != null)
			{
				CoreWindow.GetForCurrentThread().PointerCursor = _CoreCursol;
			}
		}


		public void PreventAutoHide()
		{
			_PrevPreventTime = DateTime.Now;
			_NextHideTime = _PrevPreventTime + Delay;

			this.AssociatedObject.Visibility = Visibility.Visible;

			var hostScreen = HostScreen;
			if (hostScreen != null)
			{
				CoreWindow.GetForCurrentThread().PointerCursor = _CoreCursol;
			}
		}


		private void EnableAutoHide()
		{
			_NextHideTime = DateTime.Now + Delay;

			_Timer = new Timer(async (state) => 
			{
				await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
				{
					if (_NextHideTime < DateTime.Now)
					{
						this.AssociatedObject.Visibility = Visibility.Collapsed;

						var hostScreen = HostScreen;
						if (WithCursol && hostScreen != null)
						{
							CoreWindow.GetForCurrentThread().PointerCursor = null;
						}
					}
				});
			}
			, this, Delay, TimeSpan.FromMilliseconds(100));

			
		}

		private void DeactivateAutoHide()
		{
			_Timer?.Dispose();
			_Timer = null;

			this.AssociatedObject.Visibility = Visibility.Visible;
		}

		CoreCursor _CoreCursol;
		DateTime _PrevPreventTime;
		DateTime _NextHideTime;
		Timer _Timer;
	}
}
