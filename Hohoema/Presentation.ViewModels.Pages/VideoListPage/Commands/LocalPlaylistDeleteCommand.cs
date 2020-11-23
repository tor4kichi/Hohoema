using I18NPortable;
using Hohoema.Models.Domain.Playlist;
using Hohoema.Presentation.Services;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hohoema.Models.UseCase.NicoVideos;

namespace Hohoema.Presentation.ViewModels.NicoVideos.Commands
{
    public sealed class LocalPlaylistDeleteCommand : DelegateCommandBase
    {
        private readonly LocalMylistManager _localMylistManager;
        private readonly DialogService _dialogService;

        public LocalPlaylistDeleteCommand(
            LocalMylistManager localMylistManager,
            DialogService dialogService
            )
        {
            _localMylistManager = localMylistManager;
            _dialogService = dialogService;
        }
        protected override bool CanExecute(object parameter)
        {
            return parameter is LocalPlaylist;
        }

        protected override async void Execute(object parameter)
        {
            var currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
            Microsoft.AppCenter.Analytics.Analytics.TrackEvent($"{currentMethod.DeclaringType.Name}#{currentMethod.Name}");

            if (parameter is LocalPlaylist localPlaylist)
            {
                if (await _dialogService.ShowMessageDialog(
                    "DeleteLocalPlaylistDescription".Translate(localPlaylist.Label),
                    "DeleteLocalPlaylistTitle".Translate(localPlaylist.Label),
                    "Delete".Translate(),
                    "Cancel".Translate()
                    ))
                {
                    _localMylistManager.RemovePlaylist(localPlaylist);

                }
            }
        }
    }
}
