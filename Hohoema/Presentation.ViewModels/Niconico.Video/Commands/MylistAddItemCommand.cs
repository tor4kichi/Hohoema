using I18NPortable;
using Hohoema.Models.Domain;
using Hohoema.Models.Domain.Niconico.LoginUser.Mylist;
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Domain.Playlist;
using Hohoema.Presentation.Services;
using Prism.Commands;
using Hohoema.Models.UseCase.NicoVideos;
using System.Linq;
using System.Collections.Generic;
using Hohoema.Dialogs;
using Uno.Extensions;
using System;
using System.Threading.Tasks;

namespace Hohoema.Presentation.ViewModels.Niconico.Video.Commands
{
    public sealed class MylistAddItemCommand : VideoContentSelectionCommandBase
    {
        private readonly DialogService _dialogService;
        private readonly UserMylistManager _userMylistManager;

        public MylistAddItemCommand(
            NotificationService notificationService,
            DialogService dialogService,
            UserMylistManager userMylistManager
            )
        {
            NotificationService = notificationService;
            _dialogService = dialogService;
            _userMylistManager = userMylistManager;
        }

        public NotificationService NotificationService { get; }
        public DialogService DialogService { get; }

        protected override void Execute(IVideoContent content)
        {
            Execute(new[] { content });
        }

        protected override async void Execute(IEnumerable<IVideoContent> items)
        {
            var currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
            Microsoft.AppCenter.Analytics.Analytics.TrackEvent($"{currentMethod.DeclaringType.Name}#{currentMethod.Name}");

            var targetMylist = _userMylistManager.Mylists.Any() ?
                    await _dialogService.ShowSingleSelectDialogAsync(
                    _userMylistManager.Mylists.ToList(),
                    nameof(LoginUserMylistPlaylist.Label),
                    (mylist, s) => mylist.Label.Contains(s),
                    "SelectMylist".Translate(),
                    "Select".Translate(),
                    "CreateNew".Translate(),
                    () => CreateMylistAsync()
                    )
                    : await CreateMylistAsync()
                    ;

            if (targetMylist != null)
            {
                var addedResult = await targetMylist.AddItem(items.Select(x => x.Id));
                if (addedResult.SuccessedItems.Any() && addedResult.FailedItems.Empty())
                {
//                    NotificationService.ShowLiteInAppNotification("InAppNotification_MylistAddedItems_Success".Translate(targetMylist.Label, addedResult.SuccessedItems.Count));
                }
                else
                {
//                    NotificationService.ShowLiteInAppNotification("InAppNotification_MylistAddedItems_Fail".Translate(targetMylist.Label));
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
