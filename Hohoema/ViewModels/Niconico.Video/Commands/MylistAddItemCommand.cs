#nullable enable
using Hohoema.Models.Niconico.Mylist.LoginUser;
using Hohoema.Models.Niconico.Video;
using Hohoema.Services;
using Hohoema.Services.Niconico;
using I18NPortable;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hohoema.ViewModels.Niconico.Video.Commands;

public sealed class MylistAddItemCommand : VideoContentSelectionCommandBase
{
    private readonly IMylistGroupDialogService _dialogService;
    private readonly LoginUserOwnedMylistManager _userMylistManager;

    public MylistAddItemCommand(
        INotificationService notificationService,
        IMylistGroupDialogService dialogService,
        LoginUserOwnedMylistManager userMylistManager
        )
    {
        NotificationService = notificationService;
        _dialogService = dialogService;
        _userMylistManager = userMylistManager;
    }

    public INotificationService NotificationService { get; }
    public DialogService DialogService { get; }

    protected override void Execute(IVideoContent content)
    {
        Execute(new[] { content });
    }

    protected override async void Execute(IEnumerable<IVideoContent> items)
    {
        var targetMylist = _userMylistManager.Mylists.Any() ?
                await _dialogService.ShowSingleSelectDialogAsync(
                _userMylistManager.Mylists.ToList(),
                null,
                nameof(LoginUserMylistPlaylist.Name),
                "SelectMylist".Translate(),
                "Select".Translate(),
                "CreateNew".Translate(),
                () => CreateMylistAsync(),
                (mylist, s) => mylist.Name.Contains(s)
                )
                : await CreateMylistAsync()
                ;

        if (targetMylist != null)
        {
            var addedResult = await targetMylist.AddItem(items);
            if (addedResult.SuccessedItems.Any() && addedResult.FailedItems.Any() is false)
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
