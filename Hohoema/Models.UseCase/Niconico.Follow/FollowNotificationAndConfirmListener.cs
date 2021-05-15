using Hohoema.Models.Domain.Niconico.Follow;
using Hohoema.Models.Domain.Niconico.Follow.LoginUser;
using Hohoema.Presentation.Services;
using I18NPortable;
using Microsoft.Toolkit.Mvvm.Messaging;
using Reactive.Bindings.Extensions;
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Hohoema.Models.UseCase.Niconico.Follow
{
    public sealed class FollowNotificationAndConfirmListener : IDisposable
    {
        private readonly DialogService _dialogService;
        private readonly NotificationService _notificationService;
        private readonly IMessenger _messenger;

        public void Dispose()
        {
            _messenger.UnregisterAll(this);
        }

        public FollowNotificationAndConfirmListener(
            DialogService dialogService,
            NotificationService notificationService,
            IMessenger messenger
            )
        {
            _dialogService = dialogService;
            _notificationService = notificationService;
            _messenger = messenger;

            _messenger.Register<UserFollowAddedMessage>(this, (r, m) => NotifyFollowAdded(m.Value));
            _messenger.Register<TagFollowAddedMessage>(this, (r, m) => NotifyFollowAdded(m.Value));
            _messenger.Register<MylistFollowAddedMessage>(this, (r, m) => NotifyFollowAdded(m.Value));
            _messenger.Register<ChannelFollowAddedMessage>(this, (r, m) => NotifyFollowAdded(m.Value));
            _messenger.Register<CommunityFollowAddedMessage>(this, (r, m) => NotifyFollowAdded(m.Value));

            _messenger.Register<UserFollowRemovedMessage>(this, (r, m) => NotifyFollowRemoved(m.Value));
            _messenger.Register<TagFollowRemovedMessage>(this, (r, m) => NotifyFollowRemoved(m.Value));
            _messenger.Register<MylistFollowRemovedMessage>(this, (r, m) => NotifyFollowRemoved(m.Value));
            _messenger.Register<ChannelFollowRemovedMessage>(this, (r, m) => NotifyFollowRemoved(m.Value));
            _messenger.Register<CommunityFollowRemovedMessage>(this, (r, m) => NotifyFollowRemoved(m.Value));

            _messenger.Register<UserFollowRemoveConfirmingAsyncRequestMessage>(this, (r, m) => m.Reply(ConfirmFollowRemovingAsync(m.Target)));
            _messenger.Register<TagFollowRemoveConfirmingAsyncRequestMessage>(this, (r, m) => m.Reply(ConfirmFollowRemovingAsync(m.Target)));
            _messenger.Register<MylistFollowRemoveConfirmingAsyncRequestMessage>(this, (r, m) => m.Reply(ConfirmFollowRemovingAsync(m.Target)));
            _messenger.Register<ChannelFollowRemoveConfirmingAsyncRequestMessage>(this, (r, m) => m.Reply(ConfirmFollowRemovingAsync(m.Target)));
            _messenger.Register<CommunityFollowRemoveConfirmingAsyncRequestMessage>(this, (r, m) => m.Reply(ConfirmFollowRemovingAsync(m.Target)));

           
        }

        void NotifyFollowAdded(IFollowable item)
        {
            _notificationService.ShowLiteInAppNotification_Success("FollowAddedNotification_WithItemName".Translate(item.Label));
        }

        void NotifyFollowRemoved(IFollowable item)
        {
            _notificationService.ShowLiteInAppNotification_Success("FollowRemovedNotification_WithItemName".Translate(item.Label));
        }


        Task<bool> ConfirmFollowRemovingAsync(IFollowable item)
        {
            return _dialogService.ShowMessageDialog(
                content: "ConfirmRemoveFollow_DialogDescWithItemName".Translate(item.Label), 
                title: "ConfirmRemoveFollow_DialogTitle".Translate(), 
                acceptButtonText: "RemoveFollow".Translate(),
                cancelButtonText: "Cancel".Translate()
                );
        }

    }
}
