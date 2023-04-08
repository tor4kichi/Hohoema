using Microsoft.Xaml.Interactivity;
using System;
using System.Diagnostics;
using System.Reactive.Linq;
using Windows.ApplicationModel.Core;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;

namespace Hohoema.Views.Behaviors;

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
public class PointerCursorAutoHideBehavior : Behavior<FrameworkElement>
{
    // カーソルを元に戻すためのやつ
    CoreCursor _DefaultCursor;
    Point _LastCursorPosition;

    // 自動非表示のためのタイマー
    // DispatcherTimerはUIスレッドフレンドリーなタイマー
    private readonly DispatcherQueueTimer _AutoHideTimer;

    private readonly MouseDevice _mouseDevice;
    private readonly DispatcherQueue _dispattcherQueue;

    // このビヘイビアを保持しているElement内にカーソルがあるかのフラグ
    // PointerEntered/PointerExitedで変更される
    bool _IsCursorInsideAssociatedObject = false;



    private static bool GetIsWindowActive()
    {
        return Window.Current.CoreWindow.ActivationMode == CoreWindowActivationMode.ActivatedInForeground;
    }


    public PointerCursorAutoHideBehavior()
    {
        _DefaultCursor = Window.Current.CoreWindow.PointerCursor;
        _mouseDevice = MouseDevice.GetForCurrentView();
        _dispattcherQueue = DispatcherQueue.GetForCurrentThread();            

        _AutoHideTimer = _dispattcherQueue.CreateTimer();
        _AutoHideTimer.Tick += AutoHideTimer_FireOnce;
        _AutoHideTimer.IsRepeating = false;            
    }

    IDisposable _mouseMovedDisposable;

    protected override void OnAttached()
    {
        _isUnlaoded = false;
        _AutoHideTimer.Interval = AutoHideDelay;

        _LastCursorPosition = GetPointerPosition();

        _IsCursorInsideAssociatedObject = IsCursorInWindow();
        _isAutoHideEnabledReal = IsAutoHideEnabled;
        _prevIsVisible = true;
        ResetAutoHideTimer();

        AssociatedObject.PointerEntered -= AssociatedObject_PointerEntered;
        AssociatedObject.PointerExited -= AssociatedObject_PointerExited;
        AssociatedObject.PointerEntered += AssociatedObject_PointerEntered;
        AssociatedObject.PointerExited += AssociatedObject_PointerExited;

        Window.Current.Activated -= Current_Activated;
        Window.Current.Activated += Current_Activated;

        _mouseMovedDisposable = WindowsObservable.FromEventPattern<MouseDevice, MouseEventArgs>(
            h => _mouseDevice.MouseMoved += h,
            h => _mouseDevice.MouseMoved -= h
            )
            .Sample(TimeSpan.FromMilliseconds(10))
            .Subscribe(e => 
            {
                _dispattcherQueue.TryEnqueue(() => 
                {
                    CursorSetter_MouseMoved(e.Sender, e.EventArgs);
                });
            });
            
        //_mouseDevice.MouseMoved -= CursorSetter_MouseMoved;
        //_mouseDevice.MouseMoved += CursorSetter_MouseMoved;

        AssociatedObject.Unloaded -= AssociatedObject_Unloaded;
        AssociatedObject.Unloaded += AssociatedObject_Unloaded;

        base.OnAttached();
    }

    private void Current_Activated(object sender, WindowActivatedEventArgs e)
    {
        if (e.WindowActivationState != CoreWindowActivationState.Deactivated)
        {
            ResetAutoHideTimer();                
        }
    }

    bool _isUnlaoded = false;
    private void AssociatedObject_Unloaded(object sender, RoutedEventArgs e)
    {
        _isUnlaoded = true;

        Window.Current.Activated -= Current_Activated;
        //MouseDevice.GetForCurrentView().MouseMoved -= CursorSetter_MouseMoved;
        _mouseMovedDisposable.Dispose();

        _AutoHideTimer?.Stop();
        Window.Current.CoreWindow.PointerCursor = _DefaultCursor;
    }

    protected override void OnDetaching()
    {
        _AutoHideTimer.Stop();

        AssociatedObject.PointerEntered -= AssociatedObject_PointerEntered;
        AssociatedObject.PointerExited -= AssociatedObject_PointerExited;

        AssociatedObject.Unloaded -= AssociatedObject_Unloaded;

        base.OnDetaching();
    }

    // 変数のキャプチャがバグるためstatic指定が必要
    static bool _isAutoHideEnabledReal = false;

    private void AutoHideTimer_FireOnce(object sender, object e)
    {
        var timer = sender as DispatcherQueueTimer;

        Debug.WriteLine($"_IsCursorInsideAssociatedObject: {_IsCursorInsideAssociatedObject}, _isAutoHideEnabledReal: {_isAutoHideEnabledReal}");

        if (_isUnlaoded is false
            && _IsCursorInsideAssociatedObject
            && _isAutoHideEnabledReal
            && IsCursorInWindow()
            )
        {
            (sender as DispatcherQueueTimer).Stop();
            Debug.WriteLine($"[CursorAutoHide] Auto hide cursor start.");
            SetCursorVisibility(false);
            Debug.WriteLine($"[CursorAutoHide] Auto hide cursor end.");
        }
        else
        {
            (sender as DispatcherQueueTimer).Stop();
            SetCursorVisibility(true);
        }
    }

    private void ResetAutoHideTimer()
    {
        if (_AutoHideTimer == null) { return; }

        if (_isAutoHideEnabledReal
            && _IsCursorInsideAssociatedObject
            )
        {
            _AutoHideTimer.Stop();
            //Debug.WriteLine($"[CursorAutoHide] Hide timer Reset.");

            _AutoHideTimer.Start();
            SetCursorVisibility(true);
        }
        else
        {
            if (_AutoHideTimer.IsRunning)
            {
                _AutoHideTimer.Stop();
                //Debug.WriteLine($"[CursorAutoHide] Hide timer Stop.");
            }

            SetCursorVisibility(true);
        }
    }

    bool _prevIsVisible = true;

    private void SetCursorVisibility(bool isVisible)
    {
        if (_DefaultCursor == null) { throw new InvalidOperationException($"Default cursor is can not be null."); }

        // 表示状態変化のトリガーを検出して処理する
        if (_prevIsVisible != isVisible)
        {
            if (isVisible)
            {
                Window.Current.CoreWindow.PointerCursor = _DefaultCursor;
                RestoreCursorPosition();

                Debug.WriteLine($"[CursorAutoHide] Show mouse cursor.");
            }
            else
            {
                Window.Current.CoreWindow.PointerCursor = null;
                RecordCursorPosition();

                Debug.WriteLine($"[CursorAutoHide] Hide mouse cursor.");
            }
        }

        _prevIsVisible = isVisible;
    }


    private void RecordCursorPosition()
    {
        _LastCursorPosition = GetPointerPosition();
        //Debug.WriteLine($"[CursorAutoHide] RecordCursorPosition() called.");
    }

    private void RestoreCursorPosition()
    {
        var windowBound = Window.Current.CoreWindow.Bounds;
        Window.Current.CoreWindow.PointerPosition = new Point(windowBound.Left + _LastCursorPosition.X, windowBound.Top + _LastCursorPosition.Y);
        //Debug.WriteLine($"[CursorAutoHide] RestoreCursorPosition() called.");
    }


    private void CursorSetter_MouseMoved(MouseDevice sender, MouseEventArgs args)
    {
        if (_isUnlaoded) { return; }

        RecordCursorPosition();

        // マウスホイールを動かした時等には移動していなくても呼ばれるがその場合は無視する
        if (args.MouseDelta.X == 0 && args.MouseDelta.Y == 0) { return; }

        //Debug.WriteLine($"[CursorAutoHide] Mouse moved start.");
        SetCursorVisibility(true);
        ResetAutoHideTimer();
        //Debug.WriteLine($"[CursorAutoHide] Mouse moved end.");
    }


    private void AssociatedObject_PointerEntered(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (_isUnlaoded) { return; }

        _IsCursorInsideAssociatedObject = true;

        Debug.WriteLine($"[CursorAutoHide] Pointer entered.");
    }

    private void AssociatedObject_PointerExited(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (_isUnlaoded) { return; }

        _IsCursorInsideAssociatedObject = false;

        Debug.WriteLine($"[CursorAutoHide] Pointer exited start.");
        SetCursorVisibility(true);
        ResetAutoHideTimer();

        Debug.WriteLine($"[CursorAutoHide] Pointer exited end.");
    }

    #region this code copy from VLC WinRT

    // source: https://code.videolan.org/videolan/vlc-winrt/blob/afb08b71d5989ebe03d9109c19c9aba541b37c6f/app/VLC_WinRT.Shared/Services/RunTime/MouseService.cs

    // lisence : https://code.videolan.org/videolan/vlc-winrt/blob/master/LICENSE
    /*
         Most of the media code engine is licensed under LGPL, like libVLC.
        The application is dual-licensed under GPLv2/MPL and the license might change later,
        if need be.
         */

    public static Point GetPointerPosition()
    {
        Window currentWindow = Window.Current;
        Point point;

        try
        {
            point = currentWindow.CoreWindow.PointerPosition;
        }
        catch (UnauthorizedAccessException)
        {
            return new Point(double.NegativeInfinity, double.NegativeInfinity);
        }

        Rect bounds = currentWindow.Bounds;
        return new Point(point.X - bounds.X, point.Y - bounds.Y);
    }

    public static bool IsCursorInWindow()
    {
        var pos = GetPointerPosition();
        if (pos == null) return false;


        return pos.Y > CoreApplication.GetCurrentView().TitleBar.Height &&
               pos.Y < Window.Current.Bounds.Height &&
               pos.X > 0 &&
               pos.X < Window.Current.Bounds.Width;
    }

    #endregion



    #region IsAutoHideEnabled DependencyProperty

    public static readonly DependencyProperty IsAutoHideEnabledProperty =
       DependencyProperty.Register(nameof(IsAutoHideEnabled)
               , typeof(Boolean)
               , typeof(PointerCursorAutoHideBehavior)
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
        var source = (PointerCursorAutoHideBehavior)sender;
        Debug.WriteLine($"[CursorAutoHide] IsAutoHideEnabled: {source.IsAutoHideEnabled}");
        PointerCursorAutoHideBehavior._isAutoHideEnabledReal = source.IsAutoHideEnabled;
        source.ResetAutoHideTimer();
    }

    #endregion


    #region AutoHideDelay DependencyProperty

    public static readonly DependencyProperty AutoHideDelayProperty =
      DependencyProperty.Register(nameof(AutoHideDelay)
              , typeof(TimeSpan)
              , typeof(PointerCursorAutoHideBehavior)
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
        PointerCursorAutoHideBehavior source = (PointerCursorAutoHideBehavior)sender;
        source._AutoHideTimer.Interval = source.AutoHideDelay;
    }

    #endregion

}