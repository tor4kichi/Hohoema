using Hohoema.Helpers;
using Hohoema.Services;
using NiconicoToolkit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml;

namespace Hohoema.Models.UseCase
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
                NiconicoId? maybeId = await ClipboardHelper.CheckClipboard();
                if (maybeId is not null and NiconicoId id)
                {
                    HohoemaNotificationService.ShowInAppNotification(id);
                }
            }
        }

        public void Dispose()
        {
            CurrentWindow.Activated -= CoreWindow_Activated;
        }
    }
}
