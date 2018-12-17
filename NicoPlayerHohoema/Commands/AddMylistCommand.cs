using Prism.Commands;

namespace NicoPlayerHohoema.Commands
{
    public sealed class AddMylistCommand : DelegateCommandBase
    {
        protected override bool CanExecute(object parameter)
        {
            return parameter is Interfaces.IVideoContent;
        }

        protected override async void Execute(object parameter)
        {
            if (parameter is Interfaces.IVideoContent)
            {
                var content = parameter as Interfaces.IVideoContent;

                var dialogService = HohoemaCommnadHelper.GetDialogService();
                var targetMylist = await dialogService.ChoiceMylist();
                if (targetMylist != null)
                {
                    var notificationService = HohoemaCommnadHelper.GetNotificationService();
                    var result = await targetMylist.AddMylistItem(content.Label);
                    notificationService.ShowInAppNotification(
                        Services.InAppNotificationPayload.CreateRegistrationResultNotification(
                            result ? Mntone.Nico2.ContentManageResult.Success : Mntone.Nico2.ContentManageResult.Failed,
                            "マイリスト",
                            targetMylist.Label,
                            content.Label
                            ));
                }
            }
        }
    }
}
