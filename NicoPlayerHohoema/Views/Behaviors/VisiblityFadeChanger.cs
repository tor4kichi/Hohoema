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
using Reactive.Bindings.Extensions;
using Windows.Foundation;
using System.Reactive.Linq;
using System.Reactive.Disposables;

namespace NicoPlayerHohoema.Views.Behaviors
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

            Debug.WriteLine($"{nameof(VisiblityFadeChanger)}: 自動非表示変更：{source.IsAutoHideEnabled}");
            source.ChangeVisible();
        }




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

            Debug.WriteLine($"表示切替:{IsVisible}");

            // 表示への切り替え、または非表示切り替え中に
            //if (IsVisible || (!IsVisible && _CurrentAnimation?.State == AnimationSetState.Running))
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

                Debug.WriteLine($"{_CurrentAnimation?.State}");

                _CurrentAnimation?.Dispose();
                _CurrentAnimation = null;

                if (IsAnimationEnabled)
                {
                    _CurrentAnimation = AssociatedObject.Fade(1.0f, Duration.TotalMilliseconds);

                    _CurrentAnimation?.StartAsync().ConfigureAwait(false);

                    if (IsAutoHideEnabled)
                    {
                        AutoHideSubject.OnNext(0);
                    }

                    Debug.WriteLine($"{nameof(VisiblityFadeChanger)}: 表示アニメーション開始 (自動非表示:{IsAutoHideEnabled})");
                }
                else
                {
                    AssociatedObject.Opacity = 1.0;
                    Debug.WriteLine($"{nameof(VisiblityFadeChanger)}: 表示");
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

                if (IsAnimationEnabled)
                {
                    var dispatcher = Dispatcher;

                    _CurrentAnimation = AssociatedObject.Fade(0, Duration.TotalMilliseconds);
                    _CurrentAnimation.Completed += HideAnimation_Completed;

                    _CurrentAnimation?.StartAsync().ConfigureAwait(false);

                    Debug.WriteLine($"{nameof(VisiblityFadeChanger)}: 非表示アニメーション開始");
                }
                else
                {
                    AssociatedObject.Opacity = 0.0;
                    Debug.WriteLine($"{nameof(VisiblityFadeChanger)}: 非表示");
                }
            }
        }

        private void HideAnimation_Completed(object sender, AnimationSetCompletedEventArgs e)
        {
            // 別のアニメーションが実行中の場合はキャンセル
            if (_CurrentAnimation?.State == AnimationSetState.Running) { return; }

            try
            {
                _SkipChangeVisible = true;
                IsVisible = false;
            }
            finally
            {
                _SkipChangeVisible = false;
            }

            Debug.WriteLine($"{nameof(VisiblityFadeChanger)}: 非表示アニメーション完了");
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

            _CurrentAnimation?.Dispose();
            _AutoHideThrottlingDisposer?.Dispose();
            _CompositeDisposable?.Dispose();

            this.AssociatedObject.Visibility = Visibility.Visible;
		}



	}
}
