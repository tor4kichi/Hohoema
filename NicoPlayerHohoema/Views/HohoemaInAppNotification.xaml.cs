using Prism.Events;
using System;
using Windows.UI.Xaml.Controls;
using Unity;
using System.Collections.Concurrent;
using Prism.Unity;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace NicoPlayerHohoema.Views
{
    public sealed partial class HohoemaInAppNotification : UserControl
    {
        public HohoemaInAppNotification()
        {
            this.InitializeComponent();



            var ea = App.Current.Container.Resolve<IEventAggregator>();
            var notificationEvent = ea.GetEvent<Services.InAppNotificationEvent>();
            notificationEvent.Subscribe(OnNotificationRequested, ThreadOption.UIThread);

            var notificationDismissEvent = ea.GetEvent<Services.InAppNotificationDismissEvent>();
            notificationDismissEvent.Subscribe((_) =>
            {
                LiteNotification.Dismiss();
            }, ThreadOption.UIThread);


            LiteNotification.Closed += LiteNotification_Dismissed;

        }


        static readonly TimeSpan DefaultShowDuration = TimeSpan.FromSeconds(7);

        private Services.InAppNotificationPayload _CurrentNotication;

        private ConcurrentQueue<Services.InAppNotificationPayload> NoticationRequestQueue = new ConcurrentQueue<Services.InAppNotificationPayload>();

        private void PushNextNotication(Services.InAppNotificationPayload payload)
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
                LiteNotification.Show((int)(payload.ShowDuration ?? DefaultShowDuration).TotalMilliseconds);
            }
        }

        private void OnNotificationRequested(Services.InAppNotificationPayload payload)
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
