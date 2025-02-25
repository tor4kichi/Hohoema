#nullable enable
using CommunityToolkit.Mvvm.Messaging;
using Hohoema.Helpers;
using Hohoema.Models.Notification;
using Hohoema.Services;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Hohoema.Views.Controls;

public sealed partial class HohoemaInAppNotification : UserControl
{
    public HohoemaInAppNotification()
    {
        this.InitializeComponent();

        Loaded += HohoemaInAppNotification_Loaded;
        Unloaded += HohoemaInAppNotification_Unloaded;
        LiteNotification.Closed += LiteNotification_Dismissed;

        Window.Current.CoreWindow.Activated += CoreWindow_Activated;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        _messenger = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<IMessenger>();
        _CurrentActiveWindowUIContextService = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<CurrentActiveWindowUIContextService>();
        _messenger.Register<InAppNotificationMessage>(this, (r, m) => PushNextNotication(m.Value));
        _messenger.Register<InAppNotificationDismissMessage>(this, (r, m) =>
        {
            LiteNotification.Dismiss();
        });
    }

    
    private async void HohoemaInAppNotification_Loaded(object sender, RoutedEventArgs e)
    {
        await Task.Delay(3000);
        TryNextDisplayNotication();
    }

    private void HohoemaInAppNotification_Unloaded(object sender, RoutedEventArgs e)
    {
        _messenger.Unregister<InAppNotificationMessage>(this);
        _messenger.Unregister<InAppNotificationDismissMessage>(this);
    }


    private readonly DispatcherQueue _dispatcherQueue;
    private readonly IMessenger _messenger;
    private readonly CurrentActiveWindowUIContextService _CurrentActiveWindowUIContextService;
    static readonly TimeSpan DefaultShowDuration = DeviceTypeHelper.IsXbox ? TimeSpan.FromSeconds(25) : TimeSpan.FromSeconds(15);

    private InAppNotificationPayload _CurrentNotication;

    private ConcurrentQueue<InAppNotificationPayload> NoticationRequestQueue = new ConcurrentQueue<InAppNotificationPayload>();

    private void PushNextNotication(InAppNotificationPayload payload)
    {
        NoticationRequestQueue.Enqueue(payload);

        if (!IsLoaded) { return; }

        _dispatcherQueue.TryEnqueue(() => 
        {
            // 前に表示した通知が時間設定されていない場合には強制非表示
            if (_CurrentNotication != null && _CurrentNotication.ShowDuration == null)
            {
                LiteNotification.Dismiss();
            }
            else
            {
                TryNextDisplayNotication();
            }
        });
    }



    private void CoreWindow_Activated(Windows.UI.Core.CoreWindow sender, Windows.UI.Core.WindowActivatedEventArgs args)
    {
        _lastActivationState = args.WindowActivationState;
    }

    Windows.UI.Core.CoreWindowActivationState _lastActivationState = Windows.UI.Core.CoreWindowActivationState.CodeActivated;
    private void TryNextDisplayNotication()
    {
        // only show Active Window
        if (_CurrentActiveWindowUIContextService.UIContext == null)
        {
            return;
        }

        if (_CurrentActiveWindowUIContextService.UIContext != UIContext)
        {
            NoticationRequestQueue.Clear();
            return;
        }
                    
        if (NoticationRequestQueue.TryDequeue(out var payload))
        {
            if (payload == null) { return; }

            _CurrentNotication = payload;
            
            LiteNotification.DataContext = payload;
            LiteNotification.ShowDismissButton = payload.IsShowDismissButton;
            LiteNotification.Show((int)(payload.ShowDuration ?? DefaultShowDuration).TotalMilliseconds);                
        }
    }

    private void LiteNotification_Dismissed(object sender, EventArgs e)
    {
        _CurrentNotication = null;
        (sender as Microsoft.Toolkit.Uwp.UI.Controls.InAppNotification).DataContext = null;
        TryNextDisplayNotication();
    }

    public void Receive(InAppNotificationMessage message)
    {
        PushNextNotication(message.Value);
    }

    object _lastFocusedElement;
    public void TryFocusOrDismissWhenNoCommands()
    {
        if (TrySetFocus()) { return; }

        LiteNotification.Dismiss();
        TryNextDisplayNotication();            
    }

    public void Dismiss()
    {
        if (_lastFocusedElement is DependencyObject lastFocusedDep)
        {
            _ = FocusManager.TryFocusAsync(lastFocusedDep, FocusState.Programmatic);
        }

        LiteNotification.Dismiss();
        TryNextDisplayNotication();
    }

    public bool TrySetFocus()
    {
        _lastFocusedElement = FocusManager.GetFocusedElement();
        if (LiteNotification.DataContext is InAppNotificationPayload payload)
        {
            if (payload.Commands.Any())
            {
                var button = CommandsItemsControl.FindFirstChild<Button>();
                button.Focus(FocusState.Programmatic);
                return true;
            }
        }

        return false;
    }
}


