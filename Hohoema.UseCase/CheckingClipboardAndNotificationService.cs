using Hohoema.Models.Helpers;
using Hohoema.UseCase.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml;

namespace Hohoema.Services.Notification
{
    public sealed class CheckingClipboardAndNotificationService : IDisposable
    {
        private readonly IInAppNotificationService _inAppNotificationService;
        private CoreWindow CurrentWindow { get; }

        public CheckingClipboardAndNotificationService(
            IInAppNotificationService inAppNotificationService
            )
        {
            _inAppNotificationService = inAppNotificationService;

            // ウィンドウを有効化したタイミングでクリップボードをチェックする
            CurrentWindow = Window.Current.CoreWindow;
            CurrentWindow.Activated += CoreWindow_Activated;
        }

        private async void CoreWindow_Activated(Windows.UI.Core.CoreWindow sender, Windows.UI.Core.WindowActivatedEventArgs args)
        {
            if (args.WindowActivationState == CoreWindowActivationState.PointerActivated)
            {
                var clipboard = await ClipboardHelper.CheckClipboard();
                if (clipboard != null)
                {
                    _inAppNotificationService.ShowInAppNotification(clipboard.Type, clipboard.Id);
                }
            }
        }

        public void Dispose()
        {
            CurrentWindow.Activated -= CoreWindow_Activated;
        }
    }
}
