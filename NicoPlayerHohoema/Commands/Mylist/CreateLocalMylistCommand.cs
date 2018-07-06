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
            return true;
        }

        protected override async void Execute(object parameter)
        {
            var hohoemaApp = HohoemaCommnadHelper.GetHohoemaApp();
            var playlist = hohoemaApp.Playlist;

            var dialogService = App.Current.Container.Resolve<Services.HohoemaDialogService>();
            var data = new Dialogs.MylistGroupEditData() { };
            var result = await dialogService.GetTextAsync("新しいローカルマイリストの名前は…", "", "新しいローカルマイリスト", (s) => !string.IsNullOrWhiteSpace(s));
            if (result != null)
            {
                var newMylist = playlist.CreatePlaylist(Guid.NewGuid().ToString(), result);

                Debug.WriteLine("ローカルマイリスト作成：" + newMylist.Label);

                if (parameter is Interfaces.IVideoContent video)
                {
                    if (newMylist.AddItemCommand?.CanExecute(parameter) ?? false)
                    {
                        newMylist.AddItemCommand.Execute(parameter);
                    }
                }
            }

            
        }
    }
}
