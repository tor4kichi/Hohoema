using Hohoema.Dialogs;
using Hohoema.Models.Niconico.Mylist.LoginUser;
using Hohoema.Models.Niconico.Video;
using Hohoema.Models.UseCase.Playlist;
using Hohoema.Services;
using I18NPortable;
using NiconicoToolkit.Account;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.ViewModels.Niconico.Video.Commands
{
    public sealed class MylistCopyItemCommand : VideoContentSelectionCommandBase
    {
        private readonly DialogService _dialogService;
        private readonly NotificationService _notificationService;
        private readonly LoginUserOwnedMylistManager _userMylistManager;

        public MylistCopyItemCommand(
            LoginUserOwnedMylistManager userMylistManager,
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
            if (SourceMylist == null) { throw new NullReferenceException(); }

            var targetMylist = TargetMylist;
            if (targetMylist == null)
            {
                targetMylist = _userMylistManager.Mylists.Any() ?
                    await _dialogService.ShowSingleSelectDialogAsync(
                    _userMylistManager.Mylists.Where(x => x.MylistId != SourceMylist.MylistId).ToList(),
                    nameof(LoginUserMylistPlaylist.Name),
                    (mylist, s) => mylist.Name.Contains(s),
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
                var result = await SourceMylist.CopyItemAsync(targetMylist.MylistId, items.Select(x => x.VideoId).ToArray());
                if (result != ContentManageResult.Failed)
                {
                    _notificationService.ShowLiteInAppNotification("InAppNotification_MylistCopiedItems_Success".Translate(targetMylist.Name, items.Count()));                        
                }
                else
                {
                    _notificationService.ShowLiteInAppNotification("InAppNotification_MylistCopiedItems_Fail".Translate(targetMylist.Name));
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
