using NicoPlayerHohoema.Models;
using NicoPlayerHohoema.Services;
using Prism.Commands;

namespace NicoPlayerHohoema.Commands
{
    public sealed class AddMylistCommand : DelegateCommandBase
    {
        public AddMylistCommand(
            NotificationService notificationService,
            DialogService dialogService)
        {
            NotificationService = notificationService;
            DialogService = dialogService;
        }

        public NotificationService NotificationService { get; }
        public DialogService DialogService { get; }

        protected override bool CanExecute(object parameter)
        {
            return parameter is Interfaces.IVideoContent;
        }

        protected override async void Execute(object parameter)
        {
            if (parameter is Interfaces.IVideoContent)
            {
                var content = parameter as Interfaces.IVideoContent;

                var targetMylist = await DialogService.ChoiceMylist();
                if (targetMylist != null)
                {
                    var result = await targetMylist.AddMylistItem(content.Label);
                    NotificationService.ShowInAppNotification(
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
