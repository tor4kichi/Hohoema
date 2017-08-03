using System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.Devices.Input;
using Microsoft.Xaml.Interactivity;

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
        // カーソルを元に戻すためのやつ
        CoreCursor _DefaultCursor;

        // 自動非表示のためのタイマー
        // DispatcherTimerはUIスレッドフレンドリーなタイマー
        DispatcherTimer _AutoHideTimer = new DispatcherTimer();


        // このビヘイビアを保持しているElement内にカーソルがあるかのフラグ
        // PointerEntered/PointerExitedで変更される
        bool _IsCursorInsideAssociatedObject = false;



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
            source.ResetAutoHideTimer();
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



        protected override void OnAttached()
        {
            base.OnAttached();

            _DefaultCursor = Window.Current.CoreWindow.PointerCursor;

            AssociatedObject.Loaded += AssociatedObject_Loaded;
            AssociatedObject.Unloaded += AssociatedObject_Unloaded;
        }


        private void AssociatedObject_Loaded(object sender, RoutedEventArgs e)
        {
            MouseDevice.GetForCurrentView().MouseMoved += CursorSetter_MouseMoved;

            AssociatedObject.PointerEntered += AssociatedObject_PointerEntered;
            AssociatedObject.PointerExited += AssociatedObject_PointerExited;

            _AutoHideTimer.Tick += AutoHideTimer_Tick;

            ResetAutoHideTimer();
        }


        private void AssociatedObject_Unloaded(object sender, RoutedEventArgs e)
        {
            _AutoHideTimer.Stop();

            MouseDevice.GetForCurrentView().MouseMoved -= CursorSetter_MouseMoved;

            Window.Current.CoreWindow.PointerCursor = _DefaultCursor;
        }



        private void ResetAutoHideTimer()
        {
            _AutoHideTimer.Stop();
            if (IsAutoHideEnabled)
            {
                _AutoHideTimer.Start();
            }
        }

        private void CursorVisibilityChanged(bool isVisible)
        {
            if (_DefaultCursor == null) { throw new Exception($"Default cursor is can not be null."); }

            if (isVisible)
            {
                Window.Current.CoreWindow.PointerCursor = _DefaultCursor;

                ResetAutoHideTimer();
            }
            else
            {
                Window.Current.CoreWindow.PointerCursor = null;

                _AutoHideTimer.Stop();
            }
        }


        private void AutoHideTimer_Tick(object sender, object e)
        {
            if (IsAutoHideEnabled && _IsCursorInsideAssociatedObject)
            {
                CursorVisibilityChanged(false);
            }

            _AutoHideTimer.Stop();
        }

        private void CursorSetter_MouseMoved(MouseDevice sender, MouseEventArgs args)
        {
            ResetAutoHideTimer();

            CursorVisibilityChanged(true);
        }


        private void AssociatedObject_PointerEntered(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            _IsCursorInsideAssociatedObject = true;
        }

        private void AssociatedObject_PointerExited(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            _IsCursorInsideAssociatedObject = false;

            CursorVisibilityChanged(true);
        }

    }
}
