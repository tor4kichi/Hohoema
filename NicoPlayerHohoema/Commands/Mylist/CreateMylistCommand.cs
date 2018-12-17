using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Practices.Unity;
using System.Diagnostics;

namespace NicoPlayerHohoema.Commands.Mylist
{
    public sealed class CreateMylistCommand : DelegateCommandBase
    {
        protected override bool CanExecute(object parameter)
        {
            if (parameter == null) { return false; }

            return parameter is Interfaces.IVideoContent 
                || Mntone.Nico2.NiconicoRegex.IsVideoId(parameter as string);
        }

        protected override async void Execute(object parameter)
        {
            var userMylistManager = HohoemaCommnadHelper.GetUserMylistManager();

            var dialogService = App.Current.Container.Resolve<Services.DialogService>();
            var data = new Dialogs.MylistGroupEditData() { };
            var result = await dialogService.ShowCreateMylistGroupDialogAsync(data);
            if (result)
            {
                var mylistCreateResult = await userMylistManager.AddMylist(data.Name, data.Description, data.IsPublic, data.MylistDefaultSort, data.IconType);

                Debug.WriteLine("マイリスト作成：" + mylistCreateResult);
            }

            var mylist = userMylistManager.UserMylists.FirstOrDefault(x => x.Label == data.Name);
            

            if (parameter is Interfaces.IVideoContent content)
            {
                await mylist.AddMylistItem(content.Id);
            }
            else if (parameter is string videoId)
            {
                await mylist.AddMylistItem(videoId);
            }
        }
    }
}
