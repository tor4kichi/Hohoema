using Microsoft.Xaml.Interactivity;
using System;
using System.Threading;
using Windows.UI.Core;
using Windows.UI.Xaml;

using Microsoft.Toolkit.Uwp.UI.Animations;
using System.Diagnostics;

namespace NicoPlayerHohoema.Views.Behaviors
{
	public class VisiblityFadeChanger : Behavior<FrameworkElement>
	{

        #region Delay Property

        public static readonly DependencyProperty DurationProperty =
            DependencyProperty.Register("Duration"
                    , typeof(TimeSpan)
                    , typeof(VisiblityFadeChanger)
                    , new PropertyMetadata(TimeSpan.FromSeconds(0.5), OnDurationPropertyChanged)
                );

        public TimeSpan Duration
        {
            get { return (TimeSpan)GetValue(DurationProperty); }
            set { SetValue(DurationProperty, value); }
        }

        public static void OnDurationPropertyChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            VisiblityFadeChanger source = (VisiblityFadeChanger)sender;

            var duration = source.Duration;
            source._FadeInAnimation.SetDuration(duration);
            source._FadeOutAnimation.SetDuration(duration);
        }

        #endregion

        #region Delay Property

        public static readonly DependencyProperty DelayProperty =
			DependencyProperty.Register("Delay"
					, typeof(TimeSpan)
					, typeof(VisiblityFadeChanger)
					, new PropertyMetadata(default(TimeSpan))
				);

		public TimeSpan Delay
		{
			get { return (TimeSpan)GetValue(DelayProperty); }
			set { SetValue(DelayProperty, value); }
		}


		public static void OnDelayPropertyChanged(object sender, DependencyPropertyChangedEventArgs args)
		{
			VisiblityFadeChanger source = (VisiblityFadeChanger)sender;

            var delay = source.Delay;
            source._FadeInAnimation.SetDelay(delay);
            source._FadeOutAnimation.SetDelay(delay);
        }

		#endregion





		#region WithCursor Property 

		public static readonly DependencyProperty WithCursorProperty =
			DependencyProperty.Register("WithCursor"
				, typeof(bool)
				, typeof(VisiblityFadeChanger)
				, new PropertyMetadata(true)
			);

		public bool WithCursor
		{
			get { return (bool)GetValue(WithCursorProperty); }
			set { SetValue(WithCursorProperty, value); }
		}

        #endregion

        bool IsShow
        {
            get
            {
                return this.AssociatedObject.Visibility == Visibility.Visible;
            }
        }

        AnimationSet _FadeInAnimation;
        AnimationSet _FadeOutAnimation;

        CoreCursor _EmptyCursor;
        CoreCursor _DefaultCursor;

        public void Show()
		{
            if (!IsShow)
            {
                this.AssociatedObject.Visibility = Visibility.Visible;
                CoreWindow.GetForCurrentThread().PointerCursor = _DefaultCursor;

                _FadeOutAnimation.Stop();
                _FadeInAnimation.Start();
            }
        }

        public void Hide()
        {
            if (IsShow)
            {
                _FadeInAnimation.Stop();
                _FadeOutAnimation.Start();
            }
        }


        public void Toggle()
        {
            if (IsShow)
            {
                Hide();
            }
            else
            {
                Show();
            }
        }



        private void _FadeOutAnimation_Completed(object sender, EventArgs e)
        {
            this.AssociatedObject.Visibility = Visibility.Collapsed;
            if (WithCursor)
            {
//                    CoreWindow.GetForCurrentThread().PointerCursor = _EmptyCursor;
            }
        }


        protected override void OnAttached()
        {
            base.OnAttached();

            _FadeInAnimation = AssociatedObject.Fade(1, Duration.TotalMilliseconds);
            _FadeOutAnimation = AssociatedObject.Fade(0, Duration.TotalMilliseconds);
            _FadeOutAnimation.Completed += _FadeOutAnimation_Completed;

            _DefaultCursor = CoreWindow.GetForCurrentThread().PointerCursor;
            _EmptyCursor = new CoreCursor(CoreCursorType.Custom, 1);
        }
        

        protected override void OnDetaching()
		{
			base.OnDetaching();

            _FadeInAnimation?.Dispose();
            _FadeOutAnimation?.Dispose();

            this.AssociatedObject.Visibility = Visibility.Visible;
            if (WithCursor)
            {
                CoreWindow.GetForCurrentThread().PointerCursor = _DefaultCursor;
            }
		}


	}
}
