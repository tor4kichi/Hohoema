using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity;
using System.Diagnostics;
using Hohoema.Models;
using Hohoema.Services;
using Hohoema.UseCase.Playlist;
using Hohoema.UseCase.Services;
using Hohoema.Models.Repository.Niconico.Mylist;
using Hohoema.Models.Repository;

namespace Hohoema.Commands.Mylist
{
    public sealed class CreateMylistCommand : DelegateCommandBase
    {
        public CreateMylistCommand(
            UserMylistManager userMylistManager,
            IEditMylistGroupDialogService dialogService
            )
        {
            UserMylistManager = userMylistManager;
            DialogService = dialogService;
        }

        public UserMylistManager UserMylistManager { get; }
        public IEditMylistGroupDialogService DialogService { get; }

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
                var mylistCreateResult = await UserMylistManager.AddMylist(data.Name, data.Description, data.IsPublic, data.MylistDefaultSort, data.IconType);

                Debug.WriteLine("マイリスト作成：" + mylistCreateResult);
            }

            var mylist = UserMylistManager.Mylists.FirstOrDefault(x => x.Label == data.Name);
            
            if (mylist == null) { return; }

            if (parameter is IVideoContent content)
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
