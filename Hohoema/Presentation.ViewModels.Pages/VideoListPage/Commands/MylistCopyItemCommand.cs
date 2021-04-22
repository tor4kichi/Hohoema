﻿using Hohoema.Dialogs;
using Hohoema.Models.Domain.Niconico.UserFeature.Mylist;
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.UseCase.NicoVideos;
using Hohoema.Presentation.Services;
using I18NPortable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Presentation.ViewModels.NicoVideos.Commands
{
    public sealed class MylistCopyItemCommand : VideoContentSelectionCommandBase
    {
        private readonly DialogService _dialogService;
        private readonly NotificationService _notificationService;
        private readonly UserMylistManager _userMylistManager;

        public MylistCopyItemCommand(
            UserMylistManager userMylistManager,
            DialogService dialogService,
            NotificationService notificationService
            )
        {
            _userMylistManager = userMylistManager;
            _dialogService = dialogService;
            _notificationService = notificationService;
        }

        public LoginUserMylistPlaylist SourceMylist { get; set; }
        public LoginUserMylistPlaylist TargetMylist { get; set; }

        protected override void Execute(IVideoContent content)
        {
            Execute(new[] { content });
        }

        protected override async void Execute(IEnumerable<IVideoContent> items)
        {
            var currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
            Microsoft.AppCenter.Analytics.Analytics.TrackEvent($"{currentMethod.DeclaringType.Name}#{currentMethod.Name}");

            if (SourceMylist == null) { throw new NullReferenceException(); }

            var targetMylist = TargetMylist;
            if (targetMylist == null)
            {
                targetMylist = _userMylistManager.Mylists.Any() ?
                    await _dialogService.ShowSingleSelectDialogAsync(
                    _userMylistManager.Mylists.Where(x => x.Id != SourceMylist.Id).ToList(),
                    nameof(LoginUserMylistPlaylist.Label),
                    (mylist, s) => mylist.Label.Contains(s),
                    "SelectCopyTargetMylist".Translate(),
                    "Select".Translate(),
                    "CreateNew".Translate(),
                    () => CreateMylistAsync()
                    )
                    : await CreateMylistAsync()
                    ;
            }

            if (targetMylist != null)
            {
                var result = await SourceMylist.CopyItemAsync(targetMylist.Id, items.Select(x => x.Id).ToArray());
                if (result != Mntone.Nico2.ContentManageResult.Failed)
                {
                    _notificationService.ShowLiteInAppNotification("InAppNotification_MylistCopiedItems_Success".Translate(targetMylist.Label, items.Count()));                        
                }
                else
                {
                    _notificationService.ShowLiteInAppNotification("InAppNotification_MylistCopiedItems_Fail".Translate(targetMylist.Label));
                }
            }
        }

        async Task<LoginUserMylistPlaylist> CreateMylistAsync()
        {
            // 新規作成
            var data = new MylistGroupEditData();
            if (await _dialogService.ShowEditMylistGroupDialogAsync(data))
            {
                return await _userMylistManager.AddMylist(
                 data.Name,
                 data.Description,
                 data.IsPublic,
                 data.DefaultSortKey,
                 data.DefaultSortOrder
                 );
            }
            else
            {
                return default;
            }
        }
    }
}
