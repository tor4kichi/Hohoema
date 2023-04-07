using Hohoema.Models.Playlist;
using Hohoema.Models.UseCase.Playlist;
using Hohoema.Services;
using CommunityToolkit.Mvvm.Messaging;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.UseCase.Niconico.Player
{
    public sealed class CloseToastNotificationWhenPlayStarted : IDisposable
        , IRecipient<VideoPlayRequestMessage>
    {
        private readonly NotificationService _notificationService;
        private readonly IMessenger _messenger;
        private readonly IDisposable _subscriber;

        public CloseToastNotificationWhenPlayStarted(
            NotificationService notificationService,
            IMessenger messenger
            )
        {
            _notificationService = notificationService;
            _messenger = messenger;
            _messenger.Register(this);
        }

        public void Dispose()
        {
            _subscriber.Dispose();
        }

        public void Receive(VideoPlayRequestMessage message)
        {
            _notificationService.HideToast();
        }
    }
}
