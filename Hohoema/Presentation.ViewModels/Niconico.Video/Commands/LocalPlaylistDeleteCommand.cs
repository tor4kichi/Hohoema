using I18NPortable;
using Hohoema.Models.Domain.Playlist;
using Hohoema.Presentation.Services;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hohoema.Models.UseCase.Playlist;
using Hohoema.Models.UseCase.Hohoema.LocalMylist;
using Hohoema.Models.Domain.LocalMylist;

namespace Hohoema.Presentation.ViewModels.Niconico.Video.Commands
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
                    "DeleteLocalPlaylistDescription".Translate(localPlaylist.Name),
                    "DeleteLocalPlaylistTitle".Translate(localPlaylist.Name),
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
