using Microsoft.Xaml.Interactivity;
using System;
using System.Threading;
using Windows.UI.Core;
using Windows.UI.Xaml;

using Microsoft.Toolkit.Uwp.UI.Animations;
using System.Diagnostics;
using Windows.UI.Xaml.Input;

namespace NicoPlayerHohoema.Views.Behaviors
{
	public class VisiblityFadeChanger : Behavior<FrameworkElement>
	{


        AnimationSet _FadeInAnimation;
        AnimationSet _FadeOutAnimation;




        #region AutoHide

        public static readonly DependencyProperty IsAutoHideEnabledProperty =
           DependencyProperty.Register("IsAutoHideEnabled"
                   , typeof(bool)
                   , typeof(VisiblityFadeChanger)
                   , new PropertyMetadata(true, OnIsAutoHideEnabledPropertyChanged)
               );

        public bool IsAutoHideEnabled
        {
            get { return (bool)GetValue(IsAutoHideEnabledProperty); }
            set { SetValue(IsAutoHideEnabledProperty, value); }
        }


        public static void OnIsAutoHideEnabledPropertyChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            VisiblityFadeChanger source = (VisiblityFadeChanger)sender;

            if (source.IsAutoHideEnabled)
            {
                source.IsVisible = false;
            }
            else
            {
                source.IsVisible = true;
                source.AutoHideTimer.Stop();
            }
        }

        #endregion 

       


        DispatcherTimer AutoHideTimer = new DispatcherTimer();

        public void PreventAutoHide()
        {
            if (IsAutoHideEnabled)
            {
                IsVisible = true;

                AutoHideTimer.Stop();
                AutoHideTimer.Start();
            }
        }

        private void AutoHideTimer_Tick(object sender, object e)
        {
            IsVisible = false;
        }





        #region Duration Property

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

        #region IsAnimationEnable Property

        public static readonly DependencyProperty IsAnimationEnableProperty =
            DependencyProperty.Register("IsAnimationEnable"
                    , typeof(bool)
                    , typeof(VisiblityFadeChanger)
                    , new PropertyMetadata(true, OnDurationPropertyChanged)
                );

        public bool IsAnimationEnable
        {
            get { return (bool)GetValue(IsAnimationEnableProperty); }
            set { SetValue(IsAnimationEnableProperty, value); }
        }


        #endregion


        #region Delay Property

        public static readonly DependencyProperty DelayProperty =
			DependencyProperty.Register("Delay"
					, typeof(TimeSpan)
					, typeof(VisiblityFadeChanger)
					, new PropertyMetadata(default(TimeSpan), OnDelayPropertyChanged)
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
            source.AutoHideTimer.Interval = delay;

        }

        #endregion



        #region Delay Property

        public static readonly DependencyProperty IsVisibleProperty =
            DependencyProperty.Register("IsVisible"
                    , typeof(bool)
                    , typeof(VisiblityFadeChanger)
                    , new PropertyMetadata(true, OnIsVisiblePropertyChanged)
                );

        public bool IsVisible
        {
            get { return (bool)GetValue(IsVisibleProperty); }
            set { SetValue(IsVisibleProperty, value); }
        }

        public static void OnIsVisiblePropertyChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            VisiblityFadeChanger source = (VisiblityFadeChanger)sender;

            source.ChangeVisible();
        }

        #endregion


        private void ChangeVisible()
        {
            if (IsVisible)
            {
                Show();
            }
            else
            {
                Hide();
            }
        }

        private void Show()
		{
            if (this.AssociatedObject == null) { return; }

            _FadeOutAnimation.Stop();
            if (IsAnimationEnable)
            {
                _FadeInAnimation.Start();
            }
            else
            {
                AssociatedObject.Opacity = 1.0;
            }

            AutoHideTimer.Stop();
            if (IsAutoHideEnabled)
            {
                AutoHideTimer.Start();
            }

            Debug.WriteLine("Show UI");
        }

        private async void Hide()
        {
            if (this.AssociatedObject == null) { return; }

            AutoHideTimer.Stop();

            _FadeInAnimation.Stop();
            if (IsAnimationEnable)
            {
                var dispatcher = Dispatcher;
                try
                {
                    await _FadeOutAnimation.StartAsync();
                }
                catch
                {
                    AssociatedObject.Opacity = 0.0;
                }
            }
            else
            {
                AssociatedObject.Opacity = 0.0;
            }

            Debug.WriteLine("Hide UI");

        }


        public void Toggle()
        {
            IsVisible = !IsVisible;
        }

        protected override void OnAttached()
        {
            base.OnAttached();

            _FadeInAnimation = AssociatedObject.Fade(1, Duration.TotalMilliseconds);
            _FadeOutAnimation = AssociatedObject.Fade(0, Duration.TotalMilliseconds);

            AutoHideTimer.Interval = Delay;
            AutoHideTimer.Tick += AutoHideTimer_Tick;
            
            AssociatedObject.PointerMoved += AssociatedObject_PointerMoved;
            AssociatedObject.PointerEntered += AssociatedObject_PointerEntered;
            AssociatedObject.PointerExited += AssociatedObject_PointerExited;
        }

        private void AssociatedObject_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (IsVisible)
            {
                AutoHideTimer.Stop();
                AutoHideTimer.Start();
            }
        }

        private void AssociatedObject_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
//            IsAutoHideEnabled = false;
        }

        private void AssociatedObject_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            PreventAutoHide();
        }

        protected override void OnDetaching()
		{
			base.OnDetaching();

            _FadeInAnimation?.Dispose();
            _FadeOutAnimation?.Dispose();

            AutoHideTimer.Tick -= AutoHideTimer_Tick;
            AutoHideTimer.Stop();

            this.AssociatedObject.Visibility = Visibility.Visible;
		}



	}
}
