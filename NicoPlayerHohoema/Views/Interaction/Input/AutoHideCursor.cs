using System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.Devices.Input;
using Microsoft.Xaml.Interactivity;
using Windows.Foundation;
using Windows.ApplicationModel.Core;
using Windows.UI.ViewManagement;
using NicoPlayerHohoema.Models.Helpers;

namespace NicoPlayerHohoema.Views.Behaviors
{
    /* 
     * 使い方
     * 
     * ポインターを自動非表示させたいUI要素のIntaractivity.Behaviorsに配置することで利用できます。
     * 
     * IsAutoHideEnabledに動画プレイヤー等のコントロールUIのVisibilityを反転させたBool値をBindingする使い方を想定してます。
     */

    /*
     * 実装のポイント
     * 
     * Window上のUI要素が普段トリガーするポインター関連イベントが
     * ポインターカーソルを消した場合にはトリガーされなくなります。
     * 
     * そのため、カーソルを非表示にした場合は
     * MouseDeviceからマウスの移動を検出する必要があります。
     * 
     */


    /// <summary>
    /// ポインターが操作されていない時にカーソル表示を自動非表示するビヘイビア
    /// </summary>
    public class AutoHideCursor : Behavior<FrameworkElement>
    {
        public AutoHideCursor()
        {
            _MouseDevice = MouseDevice.GetForCurrentView();
            _DefaultCursor = Window.Current.CoreWindow.PointerCursor;
        }

        // カーソルを元に戻すためのやつ
        CoreCursor _DefaultCursor;

        // 自動非表示のためのタイマー
        // DispatcherTimerはUIスレッドフレンドリーなタイマー
        DispatcherTimer _AutoHideTimer = new DispatcherTimer();


        #region IsAutoHideEnabled DependencyProperty

        public static readonly DependencyProperty IsAutoHideEnabledProperty =
           DependencyProperty.Register(nameof(IsAutoHideEnabled)
                   , typeof(Boolean)
                   , typeof(AutoHideCursor)
                   , new PropertyMetadata(true, OnIsAutoHideEnabledPropertyChanged)
               );


        /// <summary>
        /// IsAutoHideEnabledがTrueに設定されるとマウスの移動が無くなった後、
        /// AutoHideDelayに設定された時間後、カーソルが非表示に自動設定されます。
        /// </summary>
        public Boolean IsAutoHideEnabled
        {
            get { return (Boolean)GetValue(IsAutoHideEnabledProperty); }
            set { SetValue(IsAutoHideEnabledProperty, value); }
        }

        public static void OnIsAutoHideEnabledPropertyChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            var source = (AutoHideCursor)sender;
            source.Reset();
        }

        #endregion


        #region AutoHideDelay DependencyProperty

        public static readonly DependencyProperty AutoHideDelayProperty =
          DependencyProperty.Register(nameof(AutoHideDelay)
                  , typeof(TimeSpan)
                  , typeof(AutoHideCursor)
                  , new PropertyMetadata(TimeSpan.FromSeconds(1), OnAutoHideDelayPropertyChanged)
              );

        /// <summary>
        /// マウスが動かなくなってから非表示になるまでの時間を指定します。<br />
        /// Delayに0秒を設定するとユーザーのマウス操作が困難になるので注意してください。
        /// </summary>
        public TimeSpan AutoHideDelay
        {
            get { return (TimeSpan)GetValue(AutoHideDelayProperty); }
            set { SetValue(AutoHideDelayProperty, value); }
        }

        public static void OnAutoHideDelayPropertyChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            AutoHideCursor source = (AutoHideCursor)sender;
            source._AutoHideTimer.Interval = source.AutoHideDelay;
        }

        #endregion


        private void Reset()
        {
            if (IsAutoHideEnabled)
            {
                ActivateAutoHide();
            }
            else
            {
                DeactivateAutoHide();
            }
        }

        private void ActivateAutoHide()
        {
            DeactivateAutoHide();

            Window.Current.CoreWindow.PointerCursor = _DefaultCursor;

            Window.Current.Activated += Current_Activated;
            _AutoHideTimer.Tick += AutoHideTimer_Tick;
            _MouseDevice.MouseMoved += CursorSetter_MouseMoved;

            // Interval再設定することでTick呼び出しタイミングをリセット
            _AutoHideTimer.Interval = AutoHideDelay;
            _AutoHideTimer.Start();
        }


        private void DeactivateAutoHide()
        {
            Window.Current.CoreWindow.PointerCursor = _DefaultCursor;
            
            Window.Current.Activated -= Current_Activated;

            _AutoHideTimer.Tick -= AutoHideTimer_Tick;
            _MouseDevice.MouseMoved -= CursorSetter_MouseMoved;

            _AutoHideTimer.Stop();

        }


        MouseDevice _MouseDevice;

        AsyncLock _CursorDisplayUpdateLock = new AsyncLock();

        protected override void OnAttached()
        {
            AssociatedObject.Loaded += AssociatedObject_Loaded;
            AssociatedObject.Unloaded += AssociatedObject_Unloaded;

            AssociatedObject.PointerEntered += AssociatedObject_PointerEntered;
            AssociatedObject.PointerExited += AssociatedObject_PointerExited;

            base.OnAttached();
        }

        protected override void OnDetaching()
        {
            AssociatedObject.Loaded -= AssociatedObject_Loaded;
            AssociatedObject.Unloaded -= AssociatedObject_Unloaded;

            AssociatedObject.PointerEntered -= AssociatedObject_PointerEntered;
            AssociatedObject.PointerExited -= AssociatedObject_PointerExited;

            base.OnDetaching();
        }


        private async void AssociatedObject_Loaded(object sender, RoutedEventArgs e)
        {
            using (var releaser = await _CursorDisplayUpdateLock.LockAsync())
            {
                ResetAutoHideTimer();
            }
        }

        private async void AssociatedObject_Unloaded(object sender, RoutedEventArgs e)
        {
            using (var releaser = await _CursorDisplayUpdateLock.LockAsync())
            {
                DeactivateAutoHide();
            }
        }


        private async void AssociatedObject_PointerEntered(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            using (var releaser = await _CursorDisplayUpdateLock.LockAsync())
            {
                _isInsideCursolAssociatedObject = true;
                Reset();
            }
        }

        private async void AssociatedObject_PointerExited(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            using (var releaser = await _CursorDisplayUpdateLock.LockAsync())
            {
                _isInsideCursolAssociatedObject = false;
                DeactivateAutoHide();
            }
        }

        bool _isInsideCursolAssociatedObject;

        private async void Current_Activated(object sender, WindowActivatedEventArgs e)
        {
            using (var releaser = await _CursorDisplayUpdateLock.LockAsync())
            {
                var view = ApplicationView.GetForCurrentView();
                if (view.IsFullScreenMode
                    || e.WindowActivationState == CoreWindowActivationState.PointerActivated)
                {
                    Reset();
                }
                else
                {
                    DeactivateAutoHide();
                }
            }
        }




        private void ResetAutoHideTimer()
        {
            _AutoHideTimer?.Stop();
            _AutoHideTimer.Tick -= AutoHideTimer_Tick;
            
            if (IsAutoHideEnabled)
            {
                _AutoHideTimer = new DispatcherTimer();
                _AutoHideTimer.Interval = AutoHideDelay;
                _AutoHideTimer.Tick += AutoHideTimer_Tick;
            }

            Reset();
        }

        private void SetCursorVisible(bool isVisible)
        {
            if (_DefaultCursor == null)
            {
                _DefaultCursor = Window.Current.CoreWindow.PointerCursor;

                if (_DefaultCursor == null) { throw new Exception(); }
            }

            if (isVisible)
            {
                Window.Current.CoreWindow.PointerCursor = _DefaultCursor;
            }
            else
            {
                Window.Current.CoreWindow.PointerCursor = null;
            }
        }


        private async void AutoHideTimer_Tick(object sender, object e)
        {
            using (var releaser = await _CursorDisplayUpdateLock.LockAsync())
            {
                SetCursorVisible(false);

                _AutoHideTimer.Stop();
            }
        }

        private async void CursorSetter_MouseMoved(MouseDevice sender, MouseEventArgs args)
        {
            using (var releaser = await _CursorDisplayUpdateLock.LockAsync())
            {
                if (_isInsideCursolAssociatedObject)
                {
                    _AutoHideTimer.Stop();

                    // Interval再設定することでTick呼び出しタイミングをリセット
                    _AutoHideTimer.Interval = AutoHideDelay;
                    _AutoHideTimer.Start();
                }

                SetCursorVisible(true);
            }
        }
    }
}
