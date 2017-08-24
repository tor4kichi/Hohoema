using NicoPlayerHohoema.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Gaming.Input;
using Windows.UI.Xaml;

namespace NicoPlayerHohoema.Views
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

        DispatcherTimer _DispatcherTimer;

        UINavigationButtons _PrevPressingButtons;
        UINavigationButtons _ProcessedHoldingButtons;
        Dictionary<UINavigationButtons, TimeSpan> _ButtonHold = new Dictionary<UINavigationButtons, TimeSpan>();


        bool _IsDisposed;

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
            _DispatcherTimer = new DispatcherTimer();
            _DispatcherTimer.Interval = __InputPollingInterval;
            _DispatcherTimer.Tick += _DispatcherTimer_Tick;

            App.Current.EnteredBackground += Current_EnteredBackground;
            App.Current.LeavingBackground += Current_LeavingBackground; ;

            UINavigationController.UINavigationControllerAdded += UINavigationController_UINavigationControllerAdded;
            UINavigationController.UINavigationControllerRemoved += UINavigationController_UINavigationControllerRemoved;

            if (UINavigationController.UINavigationControllers.Count > 0)
            {
                ActivatePolling();
            }
        }

        private void UINavigationController_UINavigationControllerRemoved(object sender, UINavigationController e)
        {
            ActivatePolling();
        }

        private void UINavigationController_UINavigationControllerAdded(object sender, UINavigationController e)
        {
            if (UINavigationController.UINavigationControllers.Count == 0)
            {
                DeactivatePolling();
            }
        }

        private void Current_LeavingBackground(object sender, Windows.ApplicationModel.LeavingBackgroundEventArgs e)
        {
            ActivatePolling();
        }

        private void Current_EnteredBackground(object sender, Windows.ApplicationModel.EnteredBackgroundEventArgs e)
        {
            DeactivatePolling();
        }

        public void Dispose()
        {
            DeactivatePolling();

            _DispatcherTimer = null;
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

            if (Util.DeviceTypeHelper.IsMobile)
            {
                return;
            }

            _DispatcherTimer.Start();
        }

        private void DeactivatePolling()
        {
            if (_IsDisposed) { return; }

            _DispatcherTimer.Stop();
        }


        private void _DispatcherTimer_Tick(object sender, object e)
        {
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

        
        
    }
}