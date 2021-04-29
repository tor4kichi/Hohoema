using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity;
using System.Diagnostics;
using Hohoema.Models.Domain;
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Presentation.Services;
using System.Reflection;
using Hohoema.Models.UseCase.NicoVideos;

namespace Hohoema.Presentation.ViewModels.Niconico.Video.Commands
{
    public sealed class MylistCreateCommand : DelegateCommandBase
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
            var currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
            Microsoft.AppCenter.Analytics.Analytics.TrackEvent($"{currentMethod.DeclaringType.Name}#{currentMethod.Name}");

            var data = new Dialogs.MylistGroupEditData() { };
            var result = await DialogService.ShowCreateMylistGroupDialogAsync(data);
            if (result)
            {
                var mylist = await UserMylistManager.AddMylist(data.Name, data.Description, data.IsPublic, data.DefaultSortKey, data.DefaultSortOrder);
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
}
