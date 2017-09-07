using Prism.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Microsoft.Practices.Unity;
using System.Collections.Concurrent;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace NicoPlayerHohoema.Views
{
	public sealed partial class MenuNavigatePageBase : ContentControl
	{
        CoreDispatcher _UIDispatcher;
        public MenuNavigatePageBase()
		{
			this.InitializeComponent();

            this.Loading += MenuNavigatePageBase_Loading;
            this.Loaded += MenuNavigatePageBase_Loaded;


            var ea = App.Current.Container.Resolve<IEventAggregator>();
            var notificationEvent = ea.GetEvent<Models.InAppNotificationEvent>();
            notificationEvent.Subscribe(OnNotificationRequested, ThreadOption.UIThread);

            var notificationDismissEvent = ea.GetEvent<Models.InAppNotificationDismissEvent>();
            notificationDismissEvent.Subscribe((_) => 
            {
                var liteNotification = GetTemplateChild("LiteNotification") as Microsoft.Toolkit.Uwp.UI.Controls.InAppNotification;
                liteNotification.Dismiss();
            }, ThreadOption.UIThread);
        }

        private Models.InAppNotificationPayload _CurrentNotication;

        private ConcurrentQueue<Models.InAppNotificationPayload> NoticationRequestQueue = new ConcurrentQueue<Models.InAppNotificationPayload>();

        private void PushNextNotication(Models.InAppNotificationPayload payload)
        {
            NoticationRequestQueue.Enqueue(payload);

            // 前に表示した通知が時間設定されていない場合には強制非表示
            if (_CurrentNotication != null && _CurrentNotication.ShowDuration == null)
            {
                var liteNotification = GetTemplateChild("LiteNotification") as Microsoft.Toolkit.Uwp.UI.Controls.InAppNotification;
                liteNotification.Dismiss();
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
                var liteNotification = GetTemplateChild("LiteNotification") as Microsoft.Toolkit.Uwp.UI.Controls.InAppNotification;

                _CurrentNotication = payload;
                liteNotification.DataContext = payload;
                liteNotification.ShowDismissButton = payload.IsShowDismissButton;
                liteNotification.Show((int)(payload.ShowDuration?.TotalMilliseconds ?? 0));
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

        private void MenuNavigatePageBase_Loading(FrameworkElement sender, object args)
		{
			ForceChangeChildDataContext();

        }

        private void MenuNavigatePageBase_Loaded(object sender, RoutedEventArgs e)
        {
            _UIDispatcher = Dispatcher;

            UINavigationManager.Pressed += UINavigationManager_Pressed;

            var pane = GetTemplateChild("PaneLayout") as FrameworkElement;

            pane.GotFocus += RootLayout_GotFocus;
            pane.LostFocus += RootLayout_LostFocus;


            var liteNotification = GetTemplateChild("LiteNotification") as Microsoft.Toolkit.Uwp.UI.Controls.InAppNotification;
            liteNotification.Dismissed += LiteNotification_Dismissed;
        }

       

        private bool _IsFocusing = false;

        private int LeftInputCount = 0;

        private void RootLayout_GotFocus(object sender, RoutedEventArgs e)
        {
            _IsFocusing = true;
        }

        private void RootLayout_LostFocus(object sender, RoutedEventArgs e)
        {
            _IsFocusing = false;
        }



        private async void UINavigationManager_Pressed(UINavigationManager sender, UINavigationButtons buttons)
        {
            await _UIDispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => 
            {
                var splitView = GetTemplateChild("ContentSplitView") as SplitView;
                if (_IsFocusing && buttons == UINavigationButtons.Left)
                {
                    LeftInputCount++;
                    if (LeftInputCount > 1)
                    {
                        splitView.IsPaneOpen = true;
                    }
                }
                else
                {
                    LeftInputCount = 0;

                    if (buttons == UINavigationButtons.Accept || buttons == UINavigationButtons.Right)
                    {
                        splitView.IsPaneOpen = false;
                    }
                }
            });
            
            
        }

        private void ForceChangeChildDataContext()
		{
			if (Parent is FrameworkElement && Content is FrameworkElement)
			{
				var depContent = Content as FrameworkElement;
				depContent.DataContext = (Parent as FrameworkElement).DataContext;
			}
		}
	}
}
