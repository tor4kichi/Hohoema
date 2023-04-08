using System;
using System.Windows.Input;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// ユーザー コントロールの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234236 を参照してください

namespace Hohoema.Views.Controls;

public sealed partial class DelayedAutoTriggerButton : UserControl
{
    public DelayedAutoTriggerButton()
    {
        this.InitializeComponent();

        _ProgressTimer = DispatcherQueue.GetForCurrentThread().CreateTimer();
        _ProgressTimer.Interval = TimeSpan.FromMilliseconds(32);
        _ProgressTimer.Tick += _ProgressTimer_Tick;
        _ProgressTimer.IsRepeating = true;

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




    public ICommand CancelCommand
    {
        get { return (ICommand)GetValue(CancelCommandProperty); }
        set { SetValue(CancelCommandProperty, value); }
    }

    // Using a DependencyProperty as the backing store for CancelCommand.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty CancelCommandProperty =
        DependencyProperty.Register("CancelCommand", typeof(ICommand), typeof(DelayedAutoTriggerButton), new PropertyMetadata(null));




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




    private readonly DispatcherQueueTimer _ProgressTimer;

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


    private void Cancel_Internal()
    {
        if (CancelCommand?.CanExecute(null) ?? false)
        {
            CancelCommand.Execute(null);
        }

        Cancel();
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
