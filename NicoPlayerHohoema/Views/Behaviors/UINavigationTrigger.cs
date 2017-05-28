using Microsoft.Xaml.Interactivity;
using NicoPlayerHohoema.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Gaming.Input;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Markup;

namespace NicoPlayerHohoema.Views.Behaviors
{

    [Flags]
    public enum UINavigationButtons
    {
        None        = 0x00000000,

        Menu        = 0x00000001,
        View        = 0x00000002,
        Accept      = 0x00000004,
        Cancel      = 0x00000008,

        Up          = 0x00000010,
        Down        = 0x00000020,
        Left        = 0x00000040,
        Right       = 0x00000080,

        Context1    = 0x00000100,
        Context2    = 0x00000200,
        Context3    = 0x00000400,
        Context4    = 0x00000800,

        PageUp      = 0x00001000,
        PageDown    = 0x00002000,
        PageLeft    = 0x00004000,
        PageRight   = 0x00008000,

        ScrollUp    = 0x00010000,
        ScrollDown  = 0x00020000,
        ScrollLeft  = 0x00040000,
        ScrollRight = 0x00080000,
    }





    /// <summary>
    /// UINavigationControllerの入力をBehavior(Trigger)として扱えるようにします。
    /// Kindが設定されると定期検出処理が走るようになります。
    /// </summary>
    [ContentProperty(Name = "Actions")]
    public sealed class UINavigationTrigger : Behavior<FrameworkElement>
    {
        private static RequiredUINavigationButtons ToRequiredButtons(UINavigationButtons kind)
        {
            var val = (int)kind & 0x000000FF;
            return (RequiredUINavigationButtons)val;
        }

        private static OptionalUINavigationButtons ToOptionalButtons(UINavigationButtons kind)
        {
            var val = ((int)kind & 0x000FFF00) >> 8;
            return (OptionalUINavigationButtons)val;
        }


        private static readonly TimeSpan __InputPollingInterval = TimeSpan.FromMilliseconds(100);



        #region Dependency Properties

        public UINavigationButtons Kind
        {
            get { return (UINavigationButtons)GetValue(KindProperty); }
            set { SetValue(KindProperty, value); }
        }

        public static readonly DependencyProperty KindProperty =
            DependencyProperty.Register(
                nameof(Kind),
                typeof(UINavigationButtons), 
                typeof(UINavigationTrigger), 
                new PropertyMetadata(UINavigationButtons.None)
                );

        
        public bool IsEnabled
        {
            get { return (bool)GetValue(IsEnabledProperty); }
            set { SetValue(IsEnabledProperty, value); }
        }

        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.Register(
                nameof(IsEnabled), 
                typeof(bool), 
                typeof(UINavigationTrigger), 
                new PropertyMetadata(true, OnIsEnabledPropertyChanged)
                );



        public ActionCollection Actions
        {
            get
            {
                if (GetValue(ActionsProperty) == null)
                {
                    return this.Actions = new ActionCollection();
                }
                return (ActionCollection)GetValue(ActionsProperty);
            }
            set { SetValue(ActionsProperty, value); }
        }

        public static readonly DependencyProperty ActionsProperty =
            DependencyProperty.Register(
                nameof(Actions),
                typeof(ActionCollection),
                typeof(UINavigationTrigger),
                new PropertyMetadata(null));


        #endregion


        #region Event Hanlder

        public static void OnIsEnabledPropertyChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            UINavigationTrigger source = (UINavigationTrigger)sender;

            source.SetActivationState();
        }


        #endregion




        AsyncLock _InputSequenceLock = new AsyncLock();
        AsyncLock _ActivationChangeLock = new AsyncLock();
        IDisposable _TimerDisposer;

        protected override void OnAttached()
        {
            ActivatePolling();
        }


        protected override void OnDetaching()
        {
            DeactivatePolling();
        }



        private void SetActivationState()
        {
            if (IsEnabled)
            {
                ActivatePolling();
            }
            else
            {
                DeactivatePolling();
            }
        }

        /// <summary>
        /// 入力検出処理を開始する。
        /// ただし、Kindが None である場合はアクティブ化を行わない。
        /// 検出終了には DeactivatePolling を呼び出す。
        /// </summary>
        private async void ActivatePolling()
        {
            DeactivatePolling();

            using (var activationChangeLockReleaser = await _ActivationChangeLock.LockAsync())
            {
                var _kind = Kind;
                if (Kind == UINavigationButtons.None)
                {
                    return;
                }

                var _required = ToRequiredButtons(Kind);
                var _optional = ToOptionalButtons(Kind);

                bool _prevPressed = false;

                var _uiDispatcher = Window.Current.Dispatcher;
                _TimerDisposer = Observable.Timer(DateTimeOffset.Now, __InputPollingInterval)
                    .Subscribe(async _ =>
                    {
                    // 入力検知の処理を同期的に実行する
                    using (var releaser = await _InputSequenceLock.LockAsync())
                    {
                        // 全てのコントローラー入力をチェック
                        foreach (var controller in UINavigationController.UINavigationControllers)
                            {
                                var currentInput = controller.GetCurrentReading();

                            // 入力が始まった瞬間を検出
                            var requiredHasPressed = _required != RequiredUINavigationButtons.None && currentInput.RequiredButtons.HasFlag(_required);
                                var optionalHasPressed = _optional != OptionalUINavigationButtons.None && currentInput.OptionalButtons.HasFlag(_optional);

                                var someButtonPressed = requiredHasPressed || optionalHasPressed;
                                var nowTrigger = someButtonPressed && (someButtonPressed ^ _prevPressed);

                            // Debug.WriteLine($"press:{someButtonPressed}, prev:{_prevPressed}, trigger:{trigger}");

                            // トリガー検出用に前フレームの入力情報を保存
                            _prevPressed = someButtonPressed;

                            // 実行
                            if (nowTrigger)
                                {
                                // Debug.WriteLine("Action Execute: " + _kind.ToString());
                                await _uiDispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                                    {
                                        foreach (var action in Actions.Cast<IAction>())
                                        {
                                            action.Execute(this.AssociatedObject, null);
                                        }
                                    });
                                    break;
                                }
                            }
                        }
                    });
            }
        }

        private async void DeactivatePolling()
        {
            using (var activationChangeLockReleaser = await _ActivationChangeLock.LockAsync())
            {
                _TimerDisposer?.Dispose();
                _TimerDisposer = null;
            }
        }


       
    }





}
