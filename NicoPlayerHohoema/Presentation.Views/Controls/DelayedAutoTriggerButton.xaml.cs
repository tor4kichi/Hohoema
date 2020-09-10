using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows.Input;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// ユーザー コントロールの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234236 を参照してください

namespace Hohoema.Presentation.Views.Controls
{
    public sealed partial class DelayedAutoTriggerButton : UserControl
    {
        public DelayedAutoTriggerButton()
        {
            this.InitializeComponent();

            _ProgressTimer.Tick += _ProgressTimer_Tick;
            Unloaded += DelayedAutoTriggerButton_Unloaded;

            _ProgressTimer.Stop();
        }

        private void DelayedAutoTriggerButton_Unloaded(object sender, RoutedEventArgs e)
        {
            Cancel();
        }

        public object CenterContent
        {
            get { return (object)GetValue(CenterContentProperty); }
            set { SetValue(CenterContentProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CenterContent.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CenterContentProperty =
            DependencyProperty.Register("CenterContent", typeof(object), typeof(DelayedAutoTriggerButton), new PropertyMetadata(null));




        public TimeSpan? DelayTime
        {
            get { return (TimeSpan?)GetValue(DelayTimeProperty); }
            set { SetValue(DelayTimeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for DelayTime.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DelayTimeProperty =
            DependencyProperty.Register("DelayTime", typeof(TimeSpan?), typeof(DelayedAutoTriggerButton), new PropertyMetadata(TimeSpan.FromSeconds(10)));




        public ICommand Command
        {
            get { return (ICommand)GetValue(CommandProperty); }
            set { SetValue(CommandProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Command.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register("Command", typeof(ICommand), typeof(DelayedAutoTriggerButton), new PropertyMetadata(null));




        public bool IsCanceled
        {
            get { return (bool)GetValue(IsCanceledProperty); }
            set { SetValue(IsCanceledProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsCanceledPlayNextVideo.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsCanceledProperty =
            DependencyProperty.Register("IsCanceled", typeof(bool), typeof(DelayedAutoTriggerButton), new PropertyMetadata(false));




        public bool IsAutoTriggerEnabled
        {
            get { return (bool)GetValue(IsAutoTriggerEnabledProperty); }
            set { SetValue(IsAutoTriggerEnabledProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsAutoTriggerEnabled.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsAutoTriggerEnabledProperty =
            DependencyProperty.Register("IsAutoTriggerEnabled", typeof(bool), typeof(DelayedAutoTriggerButton), new PropertyMetadata(true));




        DispatcherTimer _ProgressTimer = new DispatcherTimer() { Interval = TimeSpan.FromMilliseconds(32) };

        DateTime _endTime;


        private void _ProgressTimer_Tick(object sender, object e)
        {
            if (!IsAutoTriggerEnabled)
            {
                Cancel();
                return; 
            }

            var elapsedTime = (_endTime - DateTime.Now);
            RadialProgressBar.Value = elapsedTime.TotalSeconds;

            if (elapsedTime < TimeSpan.Zero)
            {
                Cancel();

                if (Command?.CanExecute(null) ?? false)
                {
                    Command.Execute(null);
                }
            }
        }

        public void Cancel()
        {
            IsCanceled = true;
            _ProgressTimer.Stop();
        }


        public void Start()
        {
            if (!IsAutoTriggerEnabled) 
            {
                Cancel();
                return; 
            }

            if (DelayTime == null)
            {
                DelayTime = TimeSpan.FromSeconds(10);
            }
            RadialProgressBar.Value = 0.0;
            IsCanceled = false;
            _ProgressTimer.Start();
            _endTime = DateTime.Now + DelayTime.Value;
        }
    }
}
