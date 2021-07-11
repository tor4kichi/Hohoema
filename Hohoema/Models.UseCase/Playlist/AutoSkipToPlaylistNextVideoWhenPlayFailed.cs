using Hohoema.Models.Domain.Playlist;
using Hohoema.Presentation.Services;
using I18NPortable;
using Microsoft.Toolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.UseCase.Playlist
{
    public sealed class AutoSkipToPlaylistNextVideoWhenPlayFailed 
        : IRecipient<PlaybackFailedMessage>
    {
        private readonly IMessenger _messenger;
        private readonly INotificationService _notificationService;

        public AutoSkipToPlaylistNextVideoWhenPlayFailed(
            IMessenger messenger,
            INotificationService notificationService
            )
        {
            _messenger = messenger;
            _notificationService = notificationService;
            _messenger.Register(this);
        }

        public async void Receive(PlaybackFailedMessage message)
        {
            var player = message.Value.Player;

            if (await player.CanGoNextAsync())
            {
                _notificationService.ShowLiteInAppNotification_Fail($"{"CanNotPlay".Translate()}\n{message.Value.FailedReason.Translate()}");
                await player.GoNextAsync();
            }
            else
            {
                _notificationService.ShowLiteInAppNotification_Fail($"{"CanNotPlay".Translate()}\n{message.Value.FailedReason.Translate()}");
            }
        }
    }
}
