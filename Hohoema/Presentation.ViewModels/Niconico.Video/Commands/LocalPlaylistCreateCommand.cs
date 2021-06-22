using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using I18NPortable;
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Presentation.Services;
using Hohoema.Models.UseCase.Playlist;

namespace Hohoema.Presentation.ViewModels.Niconico.Video.Commands
{
    public sealed class LocalPlaylistCreateCommand : DelegateCommandBase
    {
        public LocalPlaylistCreateCommand(
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
            return true;
        }

        protected override async void Execute(object parameter)
        {
            var currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
            Microsoft.AppCenter.Analytics.Analytics.TrackEvent($"{currentMethod.DeclaringType.Name}#{currentMethod.Name}");

            var result = await DialogService.GetTextAsync(
                "LocalPlaylistCreate".Translate(),
                "LocalPlaylistNameTextBoxPlacefolder".Translate(), 
                "", 
                (s) => !string.IsNullOrWhiteSpace(s)
                );
            if (result != null)
            {
                var localPlaylist = LocalMylistManager.CreatePlaylist(result);

                Debug.WriteLine("ローカルマイリスト作成：" + result);

                if (parameter is IVideoContent content)
                {
                    localPlaylist.AddPlaylistItem(content);
                }
                else if (parameter is string itemId)
                {
                    throw new NotSupportedException();
                }
            }
        }
    }
}
