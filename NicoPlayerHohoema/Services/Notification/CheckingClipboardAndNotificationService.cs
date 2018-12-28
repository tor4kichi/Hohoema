using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml;

namespace NicoPlayerHohoema.Services.Notification
{
    public sealed class CheckingClipboardAndNotificationService : IDisposable
    {
        public CheckingClipboardAndNotificationService(
            HohoemaNotificationService hohoemaNotificationService
            )
        {
            HohoemaNotificationService = hohoemaNotificationService;

            // ウィンドウを有効化したタイミングでクリップボードをチェックする
            CurrentWindow = Window.Current.CoreWindow;
            CurrentWindow.Activated += CoreWindow_Activated;
        }

        private CoreWindow CurrentWindow { get; }
        public HohoemaNotificationService HohoemaNotificationService { get; }

        private async void CoreWindow_Activated(Windows.UI.Core.CoreWindow sender, Windows.UI.Core.WindowActivatedEventArgs args)
        {
            if (args.WindowActivationState == CoreWindowActivationState.PointerActivated)
            {
                var clipboard = await Services.Helpers.ClipboardHelper.CheckClipboard();
                if (clipboard != null)
                {
                    HohoemaNotificationService.ShowInAppNotification(clipboard.Type, clipboard.Id);
                }
            }
        }

        public void Dispose()
        {
            CurrentWindow.Activated -= CoreWindow_Activated;
        }
    }
}
