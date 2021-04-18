using Microsoft.Xaml.Interactivity;
using System;
using System.Threading;
using Windows.UI.Core;
using Windows.UI.Xaml;

using Microsoft.Toolkit.Uwp.UI.Animations;
using Microsoft.Toolkit.Uwp.UI.Helpers;
using System.Diagnostics;
using Windows.UI.Xaml.Input;
using Hohoema.Models.Domain.Helpers;
using System.Threading.Tasks;
using Reactive.Bindings.Extensions;
using Windows.Foundation;
using System.Reactive.Linq;
using System.Reactive.Disposables;

namespace Hohoema.Presentation.Views.Behaviors
{
	public class VisiblityFadeChanger : Behavior<FrameworkElement>
	{


        #region AutoHide

        public static readonly DependencyProperty IsAutoHideEnabledProperty =
           DependencyProperty.Register(nameof(IsAutoHideEnabled)
                   , typeof(bool)
                   , typeof(VisiblityFadeChanger)
                   , new PropertyMetadata(true, OnIsAutoHideEnabledPropertyChanged)
               );

        public bool IsAutoHideEnabled
        {
            get { return (bool)GetValue(IsAutoHideEnabledProperty); }
            set { SetValue(IsAutoHideEnabledProperty, value); }
        }

        #endregion




        public static void OnIsAutoHideEnabledPropertyChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            VisiblityFadeChanger source = (VisiblityFadeChanger)sender;
            source.ChangeVisible();
        }




        #region Duration Property

        public static readonly DependencyProperty DurationProperty =
            DependencyProperty.Register("Duration"
                    , typeof(TimeSpan)
                    , typeof(VisiblityFadeChanger)
                    , new PropertyMetadata(TimeSpan.FromSeconds(0.5))
                );

        public TimeSpan Duration
        {
            get { return (TimeSpan)GetValue(DurationProperty); }
            set { SetValue(DurationProperty, value); }
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
					, new PropertyMetadata(TimeSpan.FromSeconds(3), OnDelayPropertyChanged)
				);

		public TimeSpan Delay
		{
			get { return (TimeSpan)GetValue(DelayProperty); }
			set { SetValue(DelayProperty, value); }
		}


		public static void OnDelayPropertyChanged(object sender, DependencyPropertyChangedEventArgs args)
		{
			VisiblityFadeChanger source = (VisiblityFadeChanger)sender;

            source.ResetAutoHideThrottling();
        }

        #endregion


        void ResetAutoHideThrottling()
        {
            _AutoHideThrottlingDisposer?.Dispose();
            _AutoHideThrottlingDisposer = AutoHideSubject.Throttle(Delay)
                   .Subscribe(_ =>
                   {
                       var task = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                       {
                           if (IsAutoHideEnabled)
                           {
                               Hide();
                           }
                       });
                   });

        }


        #region IsVisible Property

        public static readonly DependencyProperty IsVisibleProperty =
            DependencyProperty.Register("IsVisible"
                    , typeof(bool)
                    , typeof(VisiblityFadeChanger)
                    , new PropertyMetadata(false, OnIsVisiblePropertyChanged)
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

        AsyncLock _AnimationGenerateLock = new AsyncLock();

        bool _SkipChangeVisible = false;

        public void PreventAutoHide()
        {
            if (IsAutoHideEnabled && IsVisible)
            {
                AutoHideSubject.OnNext(0);
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


        CancellationTokenSource _animCts = new CancellationTokenSource();

        private void StopAnimation()
        {
            if (_animCts == null) { return; }
            
            _animCts.Cancel();
            _animCts.Dispose();
            _animCts = null;
        }

        private CancellationToken CreateAnimationCancellationToken()
        {
            if (_animCts != null)
            {
                StopAnimation();
            }

            _animCts = new CancellationTokenSource();
            return _animCts.Token;
        }

        private async void Show()
		{
            using (await _AnimationGenerateLock.LockAsync())
            {
                if (this.AssociatedObject == null) { return; }

                StopAnimation();

                if (IsAnimationEnabled)
                {
                    var ct = CreateAnimationCancellationToken();
                    _ = AnimationBuilder.Create().Opacity(1.0).StartAsync(this.AssociatedObject, ct);

                    if (IsAutoHideEnabled)
                    {
                        AutoHideSubject.OnNext(0);
                    }
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

                StopAnimation();

                if (IsAnimationEnabled)
                {
                    var ct = CreateAnimationCancellationToken();
                    _ = AnimationBuilder.Create().Opacity(0.0).StartAsync(this.AssociatedObject, ct)
                        .ContinueWith(prevTask => HideAnimation_Completed());
                }
                else
                {
                    AssociatedObject.Opacity = 0.0;
                }
            }
        }

        private void HideAnimation_Completed()
        {
            _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => 
            {
                try
                {
                    _SkipChangeVisible = true;
                    IsVisible = false;
                }
                finally
                {
                    _SkipChangeVisible = false;
                }
            });
        }




        public void Toggle()
        {
            IsVisible = !IsVisible;
        }

        CompositeDisposable _CompositeDisposable;

        IDisposable _AutoHideThrottlingDisposer;

        protected override void OnAttached()
        {
            base.OnAttached();

            _CompositeDisposable = new CompositeDisposable();

            var associatedObject = AssociatedObject;
            AssociatedObject.ObserveDependencyProperty(FrameworkElement.OpacityProperty)
                .Subscribe(_ =>
                {
                    if (associatedObject.Opacity == 0)
                    {
                        associatedObject.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        associatedObject.Visibility = Visibility.Visible;
                    }
                })
                .AddTo(_CompositeDisposable);

            _AutoHideThrottlingDisposer = AutoHideSubject.Throttle(Delay)
                .Subscribe(_ =>
            {
                var task = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    if (IsAutoHideEnabled)
                    {
                        Hide();
                    }
                });
            });

            AssociatedObject.PointerMoved += AssociatedObject_PointerMoved;
        }

        System.Reactive.Subjects.BehaviorSubject<long> AutoHideSubject = new System.Reactive.Subjects.BehaviorSubject<long>(0);

        private void AssociatedObject_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            PreventAutoHide();
        }

        protected override void OnDetaching()
		{
			base.OnDetaching();

            _AutoHideThrottlingDisposer?.Dispose();
            _CompositeDisposable?.Dispose();

            this.AssociatedObject.Visibility = Visibility.Visible;
		}



	}
}
