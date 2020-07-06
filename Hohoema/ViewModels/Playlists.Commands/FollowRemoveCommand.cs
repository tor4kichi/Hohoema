using I18NPortable;
using Hohoema.Models;
using Hohoema.Services;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hohoema.Models.Niconico.Follow;
using Hohoema.UseCase.Services;

namespace Hohoema.UseCase.Playlist.Commands
{
    public sealed class FollowRemoveCommand : DelegateCommandBase
    {
        private readonly FollowManager _followManager;
        private readonly IMessageDialogService _dialogService;

        public FollowRemoveCommand(FollowManager followManager, IMessageDialogService dialogService)
        {
            _followManager = followManager;
            _dialogService = dialogService;
        }
        protected override bool CanExecute(object parameter)
        {
            return parameter is FollowItemInfo followItem;
        }

        protected override async void Execute(object parameter)
        {
            if (parameter is FollowItemInfo followItem)
            {
                if (await _dialogService.ShowMessageDialog(
                    "ConfirmRemoveFollow_DialogDescWithItemName".Translate(followItem.Name, followItem.FollowItemType),
                    "ConfirmRemoveFollow_DialogTitle".Translate(),
                    "Delete".Translate(),
                    "Cancel".Translate()
                    ))
                {
                    _ = _followManager.RemoveFollow(followItem.FollowItemType, followItem.Id);
                }
            }
        }
    }
}
