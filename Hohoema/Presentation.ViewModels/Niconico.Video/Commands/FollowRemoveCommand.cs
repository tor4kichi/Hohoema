using I18NPortable;
using Hohoema.Models.Domain.Niconico.LoginUser.Follow;
using Hohoema.Presentation.Services;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Presentation.ViewModels.Niconico.Video.Commands
{
    public sealed class FollowRemoveCommand : DelegateCommandBase
    {
        private readonly FollowManager _followManager;
        private readonly DialogService _dialogService;

        public FollowRemoveCommand(FollowManager followManager, DialogService dialogService)
        {
            _followManager = followManager;
            _dialogService = dialogService;
        }
        protected override bool CanExecute(object parameter)
        {
            return parameter is FollowItemInfo;
        }

        protected override async void Execute(object parameter)
        {
            var currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
            Microsoft.AppCenter.Analytics.Analytics.TrackEvent($"{currentMethod.DeclaringType.Name}#{currentMethod.Name}");

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
