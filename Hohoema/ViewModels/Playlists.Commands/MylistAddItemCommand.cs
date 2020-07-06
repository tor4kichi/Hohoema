using I18NPortable;
using Hohoema.Interfaces;
using Hohoema.Models;
using Hohoema.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hohoema.Models.Repository.Niconico.Mylist;
using Hohoema.UseCase.Services;
using Hohoema.Models.Repository;
using Hohoema.UseCase.Events;

namespace Hohoema.UseCase.Playlist.Commands
{
    public class MylistAddItemCommand : VideoContentSelectionCommandBase
    {
        private readonly LoginUserMylistPlaylist _playlist;
        private readonly IInAppNotificationService _notificationService;

        public MylistAddItemCommand(LoginUserMylistPlaylist playlist, IInAppNotificationService notificationService)
        {
            _playlist = playlist;
            _notificationService = notificationService;
        }

        protected override async void Execute(IVideoContent content)
        {
            var result = await _playlist.AddItem(content.Id);
            _notificationService.ShowInAppNotification(
                InAppNotificationPayload.CreateRegistrationResultNotification(
                    result.SuccessedItems.Any() ? ContentManageResult.Success : ContentManageResult.Failed,
                    "Mylist".Translate(),
                    _playlist.Label,
                    content.Label
                    ));
        }
    }
}
