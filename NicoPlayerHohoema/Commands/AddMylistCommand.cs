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

                var hohoemaApp = HohoemaCommnadHelper.GetHohoemaApp();
                var notificationService = HohoemaCommnadHelper.GetNotificationService();
                var targetMylist = await hohoemaApp.ChoiceMylist();
                if (targetMylist != null)
                {
                    var result = await hohoemaApp.AddMylistItem(targetMylist, content.Label, content.Id);
                    notificationService.ShowInAppNotification(
                        Services.InAppNotificationPayload.CreateRegistrationResultNotification(
                            result,
                            "マイリスト",
                            targetMylist.Label,
                            content.Label
                            ));
                }
            }
        }
    }
}
