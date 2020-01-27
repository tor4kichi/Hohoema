using NicoPlayerHohoema.Models;
using NicoPlayerHohoema.Repository.Playlist;
using NicoPlayerHohoema.Services;
using NicoPlayerHohoema.UseCase.Playlist;
using Prism.Commands;

namespace NicoPlayerHohoema.UseCase.Playlist.Commands
{
    public sealed class AddMylistCommand : DelegateCommandBase
    {
        private readonly PlaylistSelectDialogService _playlistSelectDialogService;
        private readonly LocalMylistManager _localMylistManager;
        private readonly UserMylistManager _userMylistManager;

        public AddMylistCommand(
            NotificationService notificationService,
            UseCase.Playlist.PlaylistSelectDialogService playlistSelectDialogService,
            LocalMylistManager localMylistManager,
            UserMylistManager userMylistManager
            )
        {
            NotificationService = notificationService;
            _playlistSelectDialogService = playlistSelectDialogService;
            _localMylistManager = localMylistManager;
            _userMylistManager = userMylistManager;
        }

        public NotificationService NotificationService { get; }
        public DialogService DialogService { get; }

        protected override bool CanExecute(object parameter)
        {
            return parameter is Interfaces.IVideoContent;
        }

        protected override async void Execute(object parameter)
        {
            if (parameter is Interfaces.IVideoContent content)
            {
                Mntone.Nico2.ContentManageResult result = Mntone.Nico2.ContentManageResult.Failed;
                var targetMylist = await _playlistSelectDialogService.ChoiceMylist();
                if (targetMylist is LocalPlaylist localPlaylist)
                {
                    _localMylistManager.AddPlaylistItem(localPlaylist, content);
                    result = Mntone.Nico2.ContentManageResult.Success;
                }
                else if (targetMylist is LoginUserMylistPlaylist loginUserMylist)
                {
                    var addedResult = await _userMylistManager.AddItem(loginUserMylist.Id, content.Id);
                    result = addedResult.SuccessedItems.Count > 0 ? Mntone.Nico2.ContentManageResult.Success : Mntone.Nico2.ContentManageResult.Failed;
                }

                if (targetMylist != null)
                {
                    NotificationService.ShowInAppNotification(
                            Services.InAppNotificationPayload.CreateRegistrationResultNotification(
                                result,
                                "マイリスト",
                                targetMylist.Label,
                                content.Label
                                ));
                }
            }
        }
    }
}
