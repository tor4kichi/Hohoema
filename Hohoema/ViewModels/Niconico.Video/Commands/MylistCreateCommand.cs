using Hohoema.Models.Niconico.Video;
using Hohoema.Services;
using Hohoema.Services.Niconico;
using System.Collections.Generic;

namespace Hohoema.ViewModels.Niconico.Video.Commands;

public sealed class MylistCreateCommand : CommandBase
{
    public MylistCreateCommand(
        LoginUserOwnedMylistManager userMylistManager,
        DialogService dialogService
        )
    {
        UserMylistManager = userMylistManager;
        DialogService = dialogService;
    }

    public LoginUserOwnedMylistManager UserMylistManager { get; }
    public DialogService DialogService { get; }

    protected override bool CanExecute(object parameter)
    {
        return true;
    }

    protected override async void Execute(object parameter)
    {
        var data = new MylistGroupEditData() { };
        var result = await DialogService.ShowCreateMylistGroupDialogAsync(data);
        if (result)
        {
            var mylist = await UserMylistManager.AddMylist(data.Name, data.Description, data.IsPublic, data.DefaultSortKey, data.DefaultSortOrder);
            if (mylist == null) { return; }

            if (parameter is IVideoContent content)
            {
                await mylist.AddItem(content);
            }
            else if (parameter is IEnumerable<IVideoContent> items)
            {
                await mylist.AddItem(items);
            }
        }

    }
}
