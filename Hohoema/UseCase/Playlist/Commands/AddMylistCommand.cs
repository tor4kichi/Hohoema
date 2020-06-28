﻿using I18NPortable;
using Hohoema.Models;
using Hohoema.Repository.Playlist;
using Hohoema.Services;
using Hohoema.UseCase.Playlist;
using Prism.Commands;

namespace Hohoema.UseCase.Playlist.Commands
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
                    localPlaylist.AddPlaylistItem(content);
                    result = Mntone.Nico2.ContentManageResult.Success;
                }
                else if (targetMylist is LoginUserMylistPlaylist loginUserMylist)
                {
                    var addedResult = await loginUserMylist.AddItem(content.Id);
                    result = addedResult.SuccessedItems.Count > 0 ? Mntone.Nico2.ContentManageResult.Success : Mntone.Nico2.ContentManageResult.Failed;
                }

                if (targetMylist != null)
                {
                    NotificationService.ShowInAppNotification(
                            Services.InAppNotificationPayload.CreateRegistrationResultNotification(
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
