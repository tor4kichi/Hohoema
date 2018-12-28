using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Practices.Unity;
using System.Diagnostics;
using NicoPlayerHohoema.Models.LocalMylist;
using NicoPlayerHohoema.Services;

namespace NicoPlayerHohoema.Commands.Mylist
{
    public sealed class CreateLocalMylistCommand : DelegateCommandBase
    {
        public CreateLocalMylistCommand(
            LocalMylistManager localMylistManager,
            DialogService dialogService
            )
        {
            LocalMylistManager = localMylistManager;
            DialogService = dialogService;
        }

        public LocalMylistManager LocalMylistManager { get; }
        public DialogService DialogService { get; }

        protected override bool CanExecute(object parameter)
        {
            if (parameter == null) { return false; }

            return parameter is Interfaces.IVideoContent || Mntone.Nico2.NiconicoRegex.IsVideoId(parameter as string);
        }

        protected override async void Execute(object parameter)
        {
            var data = new Dialogs.MylistGroupEditData() { };
            var result = await DialogService.GetTextAsync("新しいローカルマイリストを作成", "ローカルマイリスト名", "", (s) => !string.IsNullOrWhiteSpace(s));
            if (result != null)
            {
                var localMylist = new Models.LocalMylist.LocalMylistGroup(Guid.NewGuid().ToString(), result);
                LocalMylistManager.Mylists.Add(localMylist);

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
