using I18NPortable;
using NicoPlayerHohoema.Interfaces;
using NicoPlayerHohoema.Models;
using NicoPlayerHohoema.Repository.Playlist;
using NicoPlayerHohoema.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.UseCase.Playlist.Commands
{
    public class MylistAddItemCommand : VideoContentSelectionCommandBase
    {
        private readonly LoginUserMylistPlaylist _playlist;
        private readonly NotificationService _notificationService;

        public MylistAddItemCommand(LoginUserMylistPlaylist playlist, NotificationService notificationService)
        {
            _playlist = playlist;
            _notificationService = notificationService;
        }

        protected override async void Execute(IVideoContent content)
        {
            var result = await _playlist.AddItem(content.Id);
            _notificationService.ShowInAppNotification(
                Services.InAppNotificationPayload.CreateRegistrationResultNotification(
                    result.SuccessedItems.Any() ? Mntone.Nico2.ContentManageResult.Success : Mntone.Nico2.ContentManageResult.Failed,
                    "Mylist".Translate(),
                    _playlist.Label,
                    content.Label
                    ));
        }
    }
}
