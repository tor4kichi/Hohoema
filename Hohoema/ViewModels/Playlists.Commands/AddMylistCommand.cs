using I18NPortable;
using Prism.Commands;
using Hohoema.UseCase.Services;
using Hohoema.UseCase.Events;
using Hohoema.Models.Repository.Niconico.Mylist;
using Hohoema.Models.Repository;

namespace Hohoema.UseCase.Playlist.Commands
{
    public sealed class AddMylistCommand : DelegateCommandBase
    {
        private readonly PlaylistSelectDialogService _playlistSelectDialogService;
        private readonly LocalMylistManager _localMylistManager;
        private readonly UserMylistManager _userMylistManager;

        public AddMylistCommand(
            IInAppNotificationService notificationService,
            UseCase.Playlist.PlaylistSelectDialogService playlistSelectDialogService,
            LocalMylistManager localMylistManager,
            UserMylistManager userMylistManager
            )
        {
            _inAppNotificationService = notificationService;
            _playlistSelectDialogService = playlistSelectDialogService;
            _localMylistManager = localMylistManager;
            _userMylistManager = userMylistManager;
        }

        readonly private IInAppNotificationService _inAppNotificationService;
        
        protected override bool CanExecute(object parameter)
        {
            return parameter is IVideoContent;
        }

        protected override async void Execute(object parameter)
        {
            if (parameter is IVideoContent content)
            {
                ContentManageResult result = ContentManageResult.Failed;
                var targetMylist = await _playlistSelectDialogService.ChoiceMylist();
                if (targetMylist is LocalPlaylist localPlaylist)
                {
                    localPlaylist.AddPlaylistItem(content);
                    result = ContentManageResult.Success;
                }
                else if (targetMylist is LoginUserMylistPlaylist loginUserMylist)
                {
                    var addedResult = await loginUserMylist.AddItem(content.Id);
                    result = addedResult.SuccessedItems.Count > 0 ? ContentManageResult.Success : ContentManageResult.Failed;
                }

                if (targetMylist != null)
                {
                    _inAppNotificationService.ShowInAppNotification(
                            InAppNotificationPayload.CreateRegistrationResultNotification(
                                result,
                                "Mylist".Translate(),
                                targetMylist.Label,
                                content.Label
                                ));
                }
            }
        }
    }
}
