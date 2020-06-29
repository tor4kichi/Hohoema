using Hohoema.Events;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hohoema.UseCase.Services;
using Hohoema.UseCase.Events;
using Hohoema.Models.Helpers;

namespace Hohoema.Services
{
    internal sealed class InAppNotificationService : IInAppNotificationService
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly ContentSuggenstionHelper _contentSuggenstionHelper;

        public InAppNotificationService(
            IEventAggregator eventAggregator,
            ContentSuggenstionHelper contentSuggenstionHelper
            )
        {
            _eventAggregator = eventAggregator;
            _contentSuggenstionHelper = contentSuggenstionHelper;
        }

        public void ShowInAppNotification(InAppNotificationPayload payload)
        {

            var notificationEvent = _eventAggregator.GetEvent<InAppNotificationEvent>();
            notificationEvent.Publish(payload);
        }

        public void DismissInAppNotification()
        {
            var notificationDismissEvent = _eventAggregator.GetEvent<InAppNotificationDismissEvent>();
            notificationDismissEvent.Publish(0);
        }

        public async void ShowInAppNotification(ContentType type, string id)
        {
            var payload = type switch
            {
                ContentType.Video => _contentSuggenstionHelper.SubmitVideoContentSuggestion(id),
                ContentType.Live => _contentSuggenstionHelper.SubmitLiveContentSuggestion(id),
                ContentType.Mylist => _contentSuggenstionHelper.SubmitMylistContentSuggestion(id),
                ContentType.Community => _contentSuggenstionHelper.SubmitCommunityContentSuggestion(id),
                ContentType.User => _contentSuggenstionHelper.SubmitUserSuggestion(id),
                ContentType.Channel => null,
                _ => null
            };

            if (payload != null)
            {
                ShowInAppNotification(await payload);
            }
        }
    }
}
