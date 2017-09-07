using Prism.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Microsoft.Practices.Unity;
using System.Collections.Concurrent;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace NicoPlayerHohoema.Views
{
    public sealed partial class HohoemaInAppNotification : UserControl
    {
        public HohoemaInAppNotification()
        {
            this.InitializeComponent();



            var ea = App.Current.Container.Resolve<IEventAggregator>();
            var notificationEvent = ea.GetEvent<Models.InAppNotificationEvent>();
            notificationEvent.Subscribe(OnNotificationRequested, ThreadOption.UIThread);

            var notificationDismissEvent = ea.GetEvent<Models.InAppNotificationDismissEvent>();
            notificationDismissEvent.Subscribe((_) =>
            {
                LiteNotification.Dismiss();
            }, ThreadOption.UIThread);


            LiteNotification.Dismissed += LiteNotification_Dismissed;

        }

        private Models.InAppNotificationPayload _CurrentNotication;

        private ConcurrentQueue<Models.InAppNotificationPayload> NoticationRequestQueue = new ConcurrentQueue<Models.InAppNotificationPayload>();

        private void PushNextNotication(Models.InAppNotificationPayload payload)
        {
            NoticationRequestQueue.Enqueue(payload);

            // 前に表示した通知が時間設定されていない場合には強制非表示
            if (_CurrentNotication != null && _CurrentNotication.ShowDuration == null)
            {
                LiteNotification.Dismiss();
            }
            else
            {
                TryNextDisplayNotication();
            }
        }

        private void TryNextDisplayNotication()
        {
            if (NoticationRequestQueue.TryDequeue(out var payload))
            {
                _CurrentNotication = payload;
                LiteNotification.DataContext = payload;
                LiteNotification.ShowDismissButton = payload.IsShowDismissButton;
                LiteNotification.Show((int)(payload.ShowDuration?.TotalMilliseconds ?? 0));
            }
        }

        private void OnNotificationRequested(Models.InAppNotificationPayload payload)
        {
            PushNextNotication(payload);
        }

        private void LiteNotification_Dismissed(object sender, EventArgs e)
        {
            _CurrentNotication = null;
            (sender as Microsoft.Toolkit.Uwp.UI.Controls.InAppNotification).DataContext = null;
            TryNextDisplayNotication();
        }
    }

    

}
