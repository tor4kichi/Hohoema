
using Hohoema.Models.Helpers;
using Hohoema.UseCase.Events;

namespace Hohoema.UseCase.Services
{
    public interface IInAppNotificationService
    {
        void DismissInAppNotification();
        void ShowInAppNotification(InAppNotificationPayload payload);
        void ShowInAppNotification(ContentType type, string id);
    }
}