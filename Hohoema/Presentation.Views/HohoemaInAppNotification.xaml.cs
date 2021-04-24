using System;
using Windows.UI.Xaml.Controls;
using Unity;
using System.Collections.Concurrent;
using Prism.Unity;
using Windows.UI.Xaml;
using Windows.UI.ViewManagement;
using Windows.UI.Core;
using Microsoft.Toolkit.Mvvm.Messaging;
using Windows.System;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Hohoema.Presentation.Views
{
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
        }

        private void HohoemaInAppNotification_Loaded(object sender, RoutedEventArgs e)
        {
            StrongReferenceMessenger.Default.Register<Services.InAppNotificationEvent>(this, (r, m) => PushNextNotication(m.Value));
            StrongReferenceMessenger.Default.Register<Services.InAppNotificationDismissEvent>(this, (r, m) =>
            {
                LiteNotification.Dismiss();
            });
        }

        private void HohoemaInAppNotification_Unloaded(object sender, RoutedEventArgs e)
        {
            StrongReferenceMessenger.Default.Unregister<Services.InAppNotificationEvent>(this);
            StrongReferenceMessenger.Default.Unregister<Services.InAppNotificationDismissEvent>(this);
        }


        private readonly DispatcherQueue _dispatcherQueue;

        static readonly TimeSpan DefaultShowDuration = TimeSpan.FromSeconds(7);

        private Services.InAppNotificationPayload _CurrentNotication;

        private ConcurrentQueue<Services.InAppNotificationPayload> NoticationRequestQueue = new ConcurrentQueue<Services.InAppNotificationPayload>();

        private void PushNextNotication(Services.InAppNotificationPayload payload)
        {
            NoticationRequestQueue.Enqueue(payload);

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
            if (_lastActivationState == Windows.UI.Core.CoreWindowActivationState.Deactivated)
            {
                NoticationRequestQueue.Clear();
                return;
            }

            if (NoticationRequestQueue.TryDequeue(out var payload))
            {
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
    }

    

}
