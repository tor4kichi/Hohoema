using Hohoema.Models.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Gaming.Input;
using Windows.UI.Xaml;
using System.Threading;
using Windows.UI.Core;

namespace Hohoema.Presentation.Services.UINavigation
{
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




        private static UINavigationManager __Instance;

        static readonly TimeSpan __InputPollingInterval = TimeSpan.FromMilliseconds(16); 
        static readonly TimeSpan __HoldDetectTime = TimeSpan.FromSeconds(1);
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

                        if (released != UINavigationButtons.None)
                        {
                            Pressed?.Invoke(this, released);
                        }

                        // ホールド入力の検出
                        UINavigationButtons holdingButtons = UINavigationButtons.None;
                        foreach (var target in __InputDetectTargets)
                        {
                            if (pressing.HasFlag(target))
                            {
                                if (!_ProcessedHoldingButtons.HasFlag(target))
                                {
                                    var time = _ButtonHold[target] += __InputPollingInterval;

                                    if (time > __HoldDetectTime)
                                    {
                                        holdingButtons |= target;
                                        _ProcessedHoldingButtons |= target;
                                    }
                                }
                            }
                            else
                            {
                                _ButtonHold[target] = TimeSpan.Zero;
                                _ProcessedHoldingButtons = (((UINavigationButtons)0) ^ target) & _ProcessedHoldingButtons;
                            }
                        }

                        if (holdingButtons != UINavigationButtons.None)
                        {
                            Holding?.Invoke(this, holdingButtons);
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
}