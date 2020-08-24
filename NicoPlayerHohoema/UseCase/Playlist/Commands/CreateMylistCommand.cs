using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity;
using System.Diagnostics;
using NicoPlayerHohoema.Models;
using NicoPlayerHohoema.Services;

namespace NicoPlayerHohoema.Commands.Mylist
{
    public sealed class CreateMylistCommand : DelegateCommandBase
    {
        public CreateMylistCommand(
            UserMylistManager userMylistManager,
            DialogService dialogService
            )
        {
            UserMylistManager = userMylistManager;
            DialogService = dialogService;
        }

        public UserMylistManager UserMylistManager { get; }
        public DialogService DialogService { get; }

        protected override bool CanExecute(object parameter)
        {
            return true;
        }

        protected override async void Execute(object parameter)
        {
            var data = new Dialogs.MylistGroupEditData() { };
            var result = await DialogService.ShowCreateMylistGroupDialogAsync(data);
            if (result)
            {
                var mylistId = await UserMylistManager.AddMylist(data.Name, data.Description, data.IsPublic, data.DefaultSortKey, data.DefaultSortOrder);
                var mylist = UserMylistManager.Mylists.FirstOrDefault(x => x.Id == mylistId);

                if (mylist == null) { return; }

                if (parameter is Interfaces.IVideoContent content)
                {
                    await mylist.AddItem(content.Id);
                }
                else if (parameter is string videoId)
                {
                    await mylist.AddItem(videoId);
                }
            }

        }
    }
}
