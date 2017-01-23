using Microsoft.Xaml.Interactivity;
using System;
using System.Threading;
using Windows.UI.Core;
using Windows.UI.Xaml;

using Microsoft.Toolkit.Uwp.UI.Animations;
using System.Diagnostics;

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
					, new PropertyMetadata(default(TimeSpan))
				);

		public TimeSpan Delay
		{
			get { return (TimeSpan)GetValue(DelayProperty); }
			set { SetValue(DelayProperty, value); }
		}


		public static void OnDelayPropertyChanged(object sender, DependencyPropertyChangedEventArgs args)
		{
			AutoHide source = (AutoHide)sender;
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


        AnimationSet _FadeInAnimation;
        AnimationSet _AutoFadeOutAnimation;

        CoreCursor _EmptyCursor;
        CoreCursor _DefaultCursor;

        bool _isAutoFadeOutStarted = false;

        public void PreventAutoHide()
		{
            if (IsEnable && !_isAutoFadeOutStarted)
            {
                this.AssociatedObject.Visibility = Visibility.Visible;
                CoreWindow.GetForCurrentThread().PointerCursor = _DefaultCursor;

                _FadeInAnimation.Stop();
                _isAutoFadeOutStarted = false;
                _AutoFadeOutAnimation.Stop();
                _isAutoFadeOutStarted = true;
                _AutoFadeOutAnimation.Start();
                
//                Debug.WriteLine("UIの自動非表示開始を延長");
            }
        }






		protected override void OnAttached()
		{
			base.OnAttached();

            _FadeInAnimation = AssociatedObject.Fade(1, 500);
            _AutoFadeOutAnimation = AssociatedObject.Fade(1, 500)
                .Then()
                .Fade(0, 500, Delay.TotalMilliseconds);
            _AutoFadeOutAnimation.Completed += _FadeOutAnimation_Completed;

            _DefaultCursor = CoreWindow.GetForCurrentThread().PointerCursor;
            _EmptyCursor = new CoreCursor(CoreCursorType.Custom, 1);
        }

        private void _FadeOutAnimation_Completed(object sender, EventArgs e)
        {
            Debug.WriteLine("UIの自動非表示完了");

            if (IsEnable && _isAutoFadeOutStarted)
            {
                this.AssociatedObject.Visibility = Visibility.Collapsed;
                if (WithCursor)
                {
//                    CoreWindow.GetForCurrentThread().PointerCursor = _EmptyCursor;
                }
            }

            _isAutoFadeOutStarted = false;
        }

        protected override void OnDetaching()
		{
			base.OnDetaching();

            _FadeInAnimation?.Dispose();
            _AutoFadeOutAnimation?.Dispose();

            this.AssociatedObject.Visibility = Visibility.Visible;
            if (WithCursor)
            {
                CoreWindow.GetForCurrentThread().PointerCursor = _DefaultCursor;
            }
		}




		private void EnableAutoHide()
		{
            Debug.WriteLine("UIの自動非表示を 有効 に");

            PreventAutoHide();
        }

		private void DisableteAutoHide()
		{
            Debug.WriteLine("UIの自動非表示を 無効 に");

            _AutoFadeOutAnimation.Stop();
            _FadeInAnimation.Start();

            _isAutoFadeOutStarted = false;

            this.AssociatedObject.Visibility = Visibility.Visible;
			CoreWindow.GetForCurrentThread().PointerCursor = _DefaultCursor;
			
		}

		
	}
}
