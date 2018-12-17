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
    public sealed class CreateLocalMylistCommand : DelegateCommandBase
    {
        protected override bool CanExecute(object parameter)
        {
            if (parameter == null) { return false; }

            return parameter is Interfaces.IVideoContent || Mntone.Nico2.NiconicoRegex.IsVideoId(parameter as string);
        }

        protected override async void Execute(object parameter)
        {
            var localMylistManager = HohoemaCommnadHelper.GetLocalMylistManager();
            
            var dialogService = App.Current.Container.Resolve<Services.DialogService>();
            var data = new Dialogs.MylistGroupEditData() { };
            var result = await dialogService.GetTextAsync("新しいローカルマイリストを作成", "ローカルマイリスト名", "", (s) => !string.IsNullOrWhiteSpace(s));
            if (result != null)
            {
                var localMylist = new Models.LocalMylist.LocalMylistGroup(Guid.NewGuid().ToString(), result);
                localMylistManager.LocalMylistGroups.Add(localMylist);

                Debug.WriteLine("ローカルマイリスト作成：" + result);

                if (parameter is Interfaces.IVideoContent content)
                {
                    await localMylist.AddMylistItem(content.Id);
                }
                else if (parameter is string itemId)
                {
                    await localMylist.AddMylistItem(itemId);
                }
            }

            
        }
    }
}
