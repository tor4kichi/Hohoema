using Hohoema.Dialogs;
using Hohoema.Models.Domain.Niconico.Mylist.LoginUser;
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.UseCase.Playlist;
using Hohoema.Presentation.Services;
using I18NPortable;
using NiconicoToolkit.Account;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Presentation.ViewModels.Niconico.Video.Commands
{
    public sealed class MylistMoveItemCommand : VideoContentSelectionCommandBase
    {
        private readonly DialogService _dialogService;
        private readonly NotificationService _notificationService;
        private readonly LoginUserOwnedMylistManager _userMylistManager;

        public MylistMoveItemCommand(
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
                    "SelectMoveTargetMylist".Translate(),
                    "Select".Translate(),
                    "CreateNew".Translate(),
                    () => CreateMylistAsync()
                    )
                    : await CreateMylistAsync()
                    ;
            }

            if (targetMylist != null)
            {
                var itemsCount = items.Count();
                var result = await SourceMylist.MoveItemAsync(targetMylist.MylistId, items.Select(x => x.VideoId).ToArray());
                if (result != ContentManageResult.Failed)
                {
                    _notificationService.ShowLiteInAppNotification_Success("InAppNotification_MylistMovedItems_Success".Translate(targetMylist.Name, itemsCount));
                }
                else
                {
                    _notificationService.ShowLiteInAppNotification_Fail("InAppNotification_MylistMovedItems_Fail".Translate(targetMylist.Name));
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
