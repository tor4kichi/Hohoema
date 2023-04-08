using Hohoema.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using Windows.Gaming.Input;
using Windows.UI.Core;
using Windows.UI.Xaml;

namespace Hohoema.Views.UINavigation;

[Flags]
public enum UINavigationButtons
{
    None = 0x00000000,

    Menu = 0x00000001,
    View = 0x00000002,
    Accept = 0x00000004,
    Cancel = 0x00000008,

    Up = 0x00000010,
    Down = 0x00000020,
    Left = 0x00000040,
    Right = 0x00000080,

    Context1 = 0x00000100,
    Context2 = 0x00000200,
    Context3 = 0x00000400,
    Context4 = 0x00000800,

    PageUp = 0x00001000,
    PageDown = 0x00002000,
    PageLeft = 0x00004000,
    PageRight = 0x00008000,

    ScrollUp = 0x00010000,
    ScrollDown = 0x00020000,
    ScrollLeft = 0x00040000,
    ScrollRight = 0x00080000,
}

public static class UINavigationButtonsExtention
{
    static public RequiredUINavigationButtons ToRequiredButtons(this UINavigationButtons kind)
    {
        var val = (int)kind & 0x000000FF;
        return (RequiredUINavigationButtons)val;
    }

    static public OptionalUINavigationButtons ToOptionalButtons(this UINavigationButtons kind)
    {
        var val = ((int)kind & 0x000FFF00) >> 8;
        return (OptionalUINavigationButtons)val;
    }
}

public static class RequiredUINavigationButtonsHelper
{
    static public UINavigationButtons ToUINavigationButtons(RequiredUINavigationButtons buttons)
    {
        return (UINavigationButtons)buttons;
    }
}

public static class OptionalUINavigationButtonsHelper
{
    static public UINavigationButtons ToUINavigationButtons(OptionalUINavigationButtons buttons)
    {
        return (UINavigationButtons)((int)buttons << 8);
    }
}

public delegate void UINavigationButtonEventHandler(UINavigationManager sender, UINavigationButtons buttons);

public class UINavigationManager : IDisposable
{

    /// <summary>
    /// ボタンを離した瞬間を通知するイベントです。
    /// </summary>
    public static event UINavigationButtonEventHandler Pressed;

    /// <summary>
    /// ボタンを押し続けた場合に通知されるイベントです。<br />
    /// 一度のボタン押下中に対して一回だけホールドを検出して通知します。
    /// </summary>
    public static event UINavigationButtonEventHandler Holding;




    private static readonly UINavigationManager __Instance;

    static readonly TimeSpan __InputPollingInterval = TimeSpan.FromMilliseconds(8); 
    static readonly TimeSpan __HoldDetectTime = TimeSpan.FromSeconds(0.3);
    static readonly UINavigationButtons[] __InputDetectTargets = ((UINavigationButtons[])Enum.GetValues(typeof(UINavigationButtons))).Skip(1).ToArray();

    Timer _PollingTimer;

    UINavigationButtons _PrevPressingButtons;
    UINavigationButtons _ProcessedHoldingButtons;
    Dictionary<UINavigationButtons, TimeSpan> _ButtonHold = new Dictionary<UINavigationButtons, TimeSpan>();

    AsyncLock _UpdateLock = new AsyncLock();

    bool _IsDisposed;


    public static bool InitialEnabling = true;

    static UINavigationManager()
    {
        __Instance = new UINavigationManager();
    }


    private UINavigationManager()
    {
        foreach (var target in __InputDetectTargets)
        {
            _ButtonHold[target] = TimeSpan.Zero;
        }

        Window.Current.Activated += Current_Activated;

        UINavigationController.UINavigationControllerAdded += UINavigationController_UINavigationControllerAdded;
        UINavigationController.UINavigationControllerRemoved += UINavigationController_UINavigationControllerRemoved;

        IsEnabled = InitialEnabling;
    }

    private bool _IsEnabled;
    public bool IsEnabled
    {
        get { return _IsEnabled; }
        set
        {
            if (_IsEnabled != value)
            {
                _IsEnabled = value;

                if (_IsEnabled)
                {
                    ActivatePolling();
                }
                else
                {
                    DeactivatePolling();
                }
            }
        }
    }

    private void Current_Activated(object sender, WindowActivatedEventArgs e)
    {
        if (e.WindowActivationState == CoreWindowActivationState.Deactivated)
        {
            DeactivatePolling();
        }
        else
        {
            ActivatePolling();
        }
    }

    private void UINavigationController_UINavigationControllerAdded(object sender, UINavigationController e)
    {
        ActivatePolling();
    }

    private void UINavigationController_UINavigationControllerRemoved(object sender, UINavigationController e)
    {
        DeactivatePolling();
    }



    public void Dispose()
    {
        _PollingTimer?.Dispose();
        _PollingTimer = null;
        _IsDisposed = true;
    }


    /// <summary>
    /// 入力検出処理を開始する。
    /// ただし、Kindが None である場合はアクティブ化を行わない。
    /// 検出終了には DeactivatePolling を呼び出す。
    /// </summary>
    private void ActivatePolling()
    {
        if (_IsDisposed) { return; }

        if (!IsEnabled) { return; }

        if (UINavigationController.UINavigationControllers.Count == 0) { return; }

        if (_PollingTimer == null)
        {
            _PollingTimer = new Timer(
                _ => _DispatcherTimer_Tick()
                , null
                , TimeSpan.Zero
                , __InputPollingInterval
                );
        }
    }

    private void DeactivatePolling()
    {
        if (_IsDisposed) { return; }

        _PollingTimer?.Dispose();
        _PollingTimer = null;
    }


    bool _NowUpdating = false;
    private async void _DispatcherTimer_Tick()
    {
        if (_NowUpdating)
        {
            return;
        }

        using (var releaser = await _UpdateLock.LockAsync())
        {
            try
            {
                _NowUpdating = true;

                // コントローラー入力をチェック
                foreach (var controller in UINavigationController.UINavigationControllers.Take(1))
                {
                    var currentInput = controller.GetCurrentReading();

                    // ボタンを離した瞬間を検出
                    var pressing = RequiredUINavigationButtonsHelper.ToUINavigationButtons(currentInput.RequiredButtons)
                        | OptionalUINavigationButtonsHelper.ToUINavigationButtons(currentInput.OptionalButtons);

                    //                var trigger = pressing & (_PrevPressingButtons ^ pressing);
                    var released = _PrevPressingButtons & (_PrevPressingButtons ^ pressing);                        
                    // ホールド入力の検出
                    foreach (var target in __InputDetectTargets)
                    {
                        if (pressing.HasFlag(target))
                        {
                            if (((int)_ProcessedHoldingButtons & (int)target) == 0)
                            {
                                var time = _ButtonHold[target] += __InputPollingInterval;

                                if (time > __HoldDetectTime)
                                {
                                    _ProcessedHoldingButtons |= target;
                                    Holding?.Invoke(this, target);
                                }

                                Debug.WriteLine($"Holding {target} @{time.TotalSeconds:F1}s");
                            }
                        }
                        else
                        {
                            _ButtonHold[target] = TimeSpan.Zero;
                            _ProcessedHoldingButtons = (UINavigationButtons)((int.MaxValue - (int)target) & (int)_ProcessedHoldingButtons);
                        }
                    }

                    if (released != UINavigationButtons.None)
                    {
                        Pressed?.Invoke(this, released);
                    }

                    // トリガー検出用に前フレームの入力情報を保存
                    _PrevPressingButtons = pressing;
                }
            }
            finally
            {
                _NowUpdating = false;
            }
        }
    }
}