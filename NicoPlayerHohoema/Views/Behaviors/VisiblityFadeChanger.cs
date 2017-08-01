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
                source.AutoCollapsedTimer.Stop();
            }
        }



        public static readonly DependencyProperty KeepVisibleTimeProperty =
          DependencyProperty.Register("KeepVisibleTime"
                  , typeof(TimeSpan)
                  , typeof(VisiblityFadeChanger)
                  , new PropertyMetadata(TimeSpan.FromSeconds(5), OnKeepVisibleTimePropertyChanged)
              );

        public TimeSpan KeepVisibleTime
        {
            get { return (TimeSpan)GetValue(KeepVisibleTimeProperty); }
            set { SetValue(KeepVisibleTimeProperty, value); }
        }


        public static void OnKeepVisibleTimePropertyChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            VisiblityFadeChanger source = (VisiblityFadeChanger)sender;

            source.AutoCollapsedTimer.Interval = source.KeepVisibleTime;
        }


        DispatcherTimer AutoCollapsedTimer = new DispatcherTimer();

        public void PreventAutoHide()
        {
            if (IsAutoHideEnabled)
            {
                IsVisible = true;

                AutoCollapsedTimer.Stop();
                AutoCollapsedTimer.Start();
            }
        }

        private void AutoCollapsedTimer_Tick(object sender, object e)
        {
            IsVisible = false;
        }


        #endregion


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

            if (source.IsVisible)
            {
                source.Show();
            }
            else
            {
                source.Hide();
            }
        }

        #endregion


        AnimationSet _FadeInAnimation;
        AnimationSet _FadeOutAnimation;

        private void Show()
		{
            if (this.AssociatedObject == null) { return; }

            _FadeOutAnimation.Stop();
            if (IsAnimationEnable)
            {
                if (IsAnimationEnable)
                {
                    _FadeInAnimation.Start();
                }
            }
            else
            {
                AssociatedObject.Opacity = 1.0;
            }

            AutoCollapsedTimer.Stop();
            if (IsAutoHideEnabled)
            {
                AutoCollapsedTimer.Start();
            }
        }

        private async void Hide()
        {
            if (this.AssociatedObject == null) { return; }

            AutoCollapsedTimer.Stop();

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

            AutoCollapsedTimer.Interval = KeepVisibleTime;
            AutoCollapsedTimer.Tick += AutoCollapsedTimer_Tick;
            AutoCollapsedTimer.Start();
        }

        

        protected override void OnDetaching()
		{
			base.OnDetaching();

            _FadeInAnimation?.Dispose();
            _FadeOutAnimation?.Dispose();

            AutoCollapsedTimer.Tick -= AutoCollapsedTimer_Tick;
            AutoCollapsedTimer.Stop();

            this.AssociatedObject.Visibility = Visibility.Visible;
		}


	}
}
