using Hohoema.Presentation.Services;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.UseCase.NicoVideos
{
    public sealed class CloseToastNotificationWhenPlayStarted : IDisposable
    {
        private readonly HohoemaPlaylist _hohoemaPlaylist;
        private readonly NotificationService _notificationService;
        private readonly IDisposable _subscriber;

        public CloseToastNotificationWhenPlayStarted(HohoemaPlaylist hohoemaPlaylist, NotificationService notificationService)
        {
            _hohoemaPlaylist = hohoemaPlaylist;
            _notificationService = notificationService;

            _subscriber =  _hohoemaPlaylist.ObserveProperty(x => x.CurrentItem)
                .Where(x => x != null)
                .Subscribe(_ => _notificationService.HideToast());
        }

        public void Dispose()
        {
            _subscriber.Dispose();
        }
    }
}
