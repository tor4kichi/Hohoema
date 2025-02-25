#nullable enable
using CommunityToolkit.Mvvm.Messaging;
using Hohoema.Contracts.Services;
using Hohoema.Models.Niconico;
using Hohoema.Models.Niconico.Follow;
using Hohoema.Models.Niconico.Follow.LoginUser;
using System;
using System.Threading.Tasks;

namespace Hohoema.Services.Niconico;

public sealed class FollowNotificationAndConfirmListener : IDisposable
{
    private readonly IDialogService _dialogService;
    private readonly INotificationService _notificationService;
    private readonly IMessenger _messenger;
    private readonly ILocalizeService _localizeService;

    public void Dispose()
    {
        _messenger.UnregisterAll(this);
    }

    public FollowNotificationAndConfirmListener(
        IDialogService dialogService,
        INotificationService notificationService,
        IMessenger messenger,
        ILocalizeService localizeService
        )
    {
        _dialogService = dialogService;
        _notificationService = notificationService;
        _messenger = messenger;
        _localizeService = localizeService;
        _messenger.Register<UserFollowAddedMessage>(this, (r, m) => NotifyFollowAdded(m.Value));
        _messenger.Register<TagFollowAddedMessage>(this, (r, m) => NotifyFollowAdded(m.Value));
        _messenger.Register<MylistFollowAddedMessage>(this, (r, m) => NotifyFollowAdded(m.Value));
        _messenger.Register<ChannelFollowAddedMessage>(this, (r, m) => NotifyFollowAdded(m.Value));

        _messenger.Register<UserFollowRemovedMessage>(this, (r, m) => NotifyFollowRemoved(m.Value));
        _messenger.Register<TagFollowRemovedMessage>(this, (r, m) => NotifyFollowRemoved(m.Value));
        _messenger.Register<MylistFollowRemovedMessage>(this, (r, m) => NotifyFollowRemoved(m.Value));
        _messenger.Register<ChannelFollowRemovedMessage>(this, (r, m) => NotifyFollowRemoved(m.Value));

        _messenger.Register<UserFollowRemoveConfirmingAsyncRequestMessage>(this, (r, m) => m.Reply(ConfirmFollowRemovingAsync(m.Target)));
        _messenger.Register<TagFollowRemoveConfirmingAsyncRequestMessage>(this, (r, m) => m.Reply(ConfirmFollowRemovingAsync(m.Target)));
        _messenger.Register<MylistFollowRemoveConfirmingAsyncRequestMessage>(this, (r, m) => m.Reply(ConfirmFollowRemovingAsync(m.Target)));
        _messenger.Register<ChannelFollowRemoveConfirmingAsyncRequestMessage>(this, (r, m) => m.Reply(ConfirmFollowRemovingAsync(m.Target)));


    }

    private void NotifyFollowAdded(IFollowable item)
    {
        _notificationService.ShowLiteInAppNotification_Success(_localizeService.Translate("FollowAddedNotification_WithItemName", item.GetLabel()));
    }

    private void NotifyFollowRemoved(IFollowable item)
    {
        _notificationService.ShowLiteInAppNotification_Success(_localizeService.Translate("FollowRemovedNotification_WithItemName", item.GetLabel()));
    }

    private Task<bool> ConfirmFollowRemovingAsync(IFollowable item)
    {
        return _dialogService.ShowMessageDialog(
            content: _localizeService.Translate("ConfirmRemoveFollow_DialogDescWithItemName", item.GetLabel()),
            title: _localizeService.Translate("ConfirmRemoveFollow_DialogTitle"),
            acceptButtonText: _localizeService.Translate("RemoveFollow"),
            cancelButtonText: _localizeService.Translate("Cancel")
            );
    }

}
