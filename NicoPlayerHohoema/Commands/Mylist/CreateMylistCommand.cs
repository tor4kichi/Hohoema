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
            return true;
        }

        protected override async void Execute(object parameter)
        {
            var hohoemaApp = HohoemaCommnadHelper.GetHohoemaApp();
            var mylistManager = hohoemaApp.UserMylistManager;

            var dialogService = App.Current.Container.Resolve<Services.HohoemaDialogService>();
            var data = new Dialogs.MylistGroupEditData() { };
            var result = await dialogService.ShowCreateMylistGroupDialogAsync(data);
            if (result)
            {
                var mylistCreateResult = await mylistManager.AddMylist(data.Name, data.Description, data.IsPublic, data.MylistDefaultSort, data.IconType);

                Debug.WriteLine("マイリスト作成：" + mylistCreateResult);
            }

            if (parameter is Interfaces.IVideoContent video)
            {
                var mylist = mylistManager.UserMylists.FirstOrDefault(x => x.Label == data.Name);
                if (mylist.AddItemCommand?.CanExecute(parameter) ?? false)
                {
                    mylist.AddItemCommand.Execute(parameter);
                }
            }
        }
    }
}
