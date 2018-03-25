using Microsoft.Xaml.Interactivity;
using System;
using System.Threading;
using Windows.UI.Core;
using Windows.UI.Xaml;

using Microsoft.Toolkit.Uwp.UI.Animations;
using System.Diagnostics;
using Windows.UI.Xaml.Input;
using NicoPlayerHohoema.Helpers;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Views.Behaviors
{
	public class VisiblityFadeChanger : Behavior<FrameworkElement>
	{


        #region AutoHide

        public static readonly DependencyProperty IsAutoHideEnabledProperty =
           DependencyProperty.Register("IsAutoHideEnabled"
                   , typeof(bool)
                   , typeof(VisiblityFadeChanger)
                   , new PropertyMetadata(true)
               );

        public bool IsAutoHideEnabled
        {
            get { return (bool)GetValue(IsAutoHideEnabledProperty); }
            set { SetValue(IsAutoHideEnabledProperty, value); }
        }

        #endregion 

       


        




        #region Duration Property

        public static readonly DependencyProperty DurationProperty =
            DependencyProperty.Register("Duration"
                    , typeof(TimeSpan)
                    , typeof(VisiblityFadeChanger)
                    , new PropertyMetadata(TimeSpan.FromSeconds(0.5), OnDuratiRaisePropertyChanged)
                );

        public TimeSpan Duration
        {
            get { return (TimeSpan)GetValue(DurationProperty); }
            set { SetValue(DurationProperty, value); }
        }

        public static void OnDuratiRaisePropertyChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            VisiblityFadeChanger source = (VisiblityFadeChanger)sender;

            var duration = source.Duration;
            source._CurrentAnimation?.SetDuration(duration);
        }

        #endregion

        #region IsAnimationEnable Property

        public static readonly DependencyProperty IsAnimationEnabledProperty =
            DependencyProperty.Register(nameof(IsAnimationEnabled)
                    , typeof(bool)
                    , typeof(VisiblityFadeChanger)
                    , new PropertyMetadata(true)
                );

        public bool IsAnimationEnabled
        {
            get { return (bool)GetValue(IsAnimationEnabledProperty); }
            set { SetValue(IsAnimationEnabledProperty, value); }
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
            source._CurrentAnimation?.SetDuration(delay);
        }

        #endregion



        #region IsVisible Property

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

        AnimationSet _CurrentAnimation;
        AsyncLock _AnimationGenerateLock = new AsyncLock();

        bool _SkipChangeVisible = false;
        DateTime _PrevPreventAutoHideTime = DateTime.Now;
        static readonly TimeSpan AutoHidePreventInterval = TimeSpan.FromMilliseconds(100);
        public void PreventAutoHide()
        {
            if (IsAutoHideEnabled && IsVisible)
            {
                var now = DateTime.Now;
                if (now - _PrevPreventAutoHideTime > AutoHidePreventInterval)
                {
                    ChangeVisible();
                    _PrevPreventAutoHideTime = now;
                }
            }
        }


        private void ChangeVisible()
        {
            if (_SkipChangeVisible) { return; }

            if (IsVisible)
            {
                Show();
            }
            else
            {
                Hide();
            }
        }


        private async void Show()
		{
            using (var releaser = await _AnimationGenerateLock.LockAsync())
            {
                if (this.AssociatedObject == null) { return; }

                AssociatedObject.Visibility = Visibility.Visible;

                _CurrentAnimation?.Dispose();
                _CurrentAnimation = null;

                await Task.Delay(10);

                if (IsAnimationEnabled)
                {
                    _CurrentAnimation = AssociatedObject.Fade(1.0f, Duration.TotalMilliseconds);

                    if (IsAutoHideEnabled)
                    {
                        _CurrentAnimation = _CurrentAnimation.Then().Fade(0, Duration.TotalMilliseconds, Delay.TotalMilliseconds);

                        _CurrentAnimation.Completed += async (sender, e) =>
                        {
                            await Task.Delay(1);
                            try
                            {
                                _SkipChangeVisible = true;
                                IsVisible = false;
                            }
                            finally
                            {
                                _SkipChangeVisible = false;
                            }
                        };
                    }

                    _CurrentAnimation?.StartAsync();
                }
                else
                {
                    AssociatedObject.Opacity = 1.0;
                }
            }
        }

        private async void Hide()
        {
            using (var releaser = await _AnimationGenerateLock.LockAsync())
            {
                if (this.AssociatedObject == null) { return; }

                _CurrentAnimation?.Dispose();
                _CurrentAnimation = null;

                await Task.Delay(10);

                if (IsAnimationEnabled)
                {
                    var dispatcher = Dispatcher;

                    _CurrentAnimation = AssociatedObject.Fade(0, Duration.TotalMilliseconds);
                    _CurrentAnimation.Completed += (sender, e) =>
                    {
                        AssociatedObject.Visibility = Visibility.Collapsed;
                    };

                    _CurrentAnimation?.StartAsync();
                }
                else
                {
                    AssociatedObject.Opacity = 0.0;
                    AssociatedObject.Visibility = Visibility.Collapsed;
                }
            }
        }


        public void Toggle()
        {
            IsVisible = !IsVisible;
        }

        protected override void OnAttached()
        {
            base.OnAttached();

//            _FadeInAnimation = AssociatedObject.Fade(1, Duration.TotalMilliseconds);
//            _FadeOutAnimation = AssociatedObject.Fade(0, Duration.TotalMilliseconds);
//            _FadeOutAnimation.Completed += _FadeOutAnimation_Completed;
            
            AssociatedObject.PointerMoved += AssociatedObject_PointerMoved;
        }

        private void AssociatedObject_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            PreventAutoHide();
        }

        protected override void OnDetaching()
		{
			base.OnDetaching();

            _CurrentAnimation?.Dispose();

            this.AssociatedObject.Visibility = Visibility.Visible;
		}



	}
}
