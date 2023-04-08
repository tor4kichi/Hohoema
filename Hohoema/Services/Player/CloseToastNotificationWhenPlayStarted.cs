#nullable enable
using CommunityToolkit.Mvvm.Messaging;
using Hohoema.Models.Playlist;
using System;

namespace Hohoema.Services.Player;

public sealed class CloseToastNotificationWhenPlayStarted : IDisposable
    , IRecipient<VideoPlayRequestMessage>
{
    private readonly NotificationService _notificationService;
    private readonly IMessenger _messenger;
    private readonly IDisposable _subscriber;

    public CloseToastNotificationWhenPlayStarted(
        NotificationService notificationService,
        IMessenger messenger
        )
    {
        _notificationService = notificationService;
        _messenger = messenger;
        _messenger.Register(this);
    }

    public void Dispose()
    {
        _subscriber.Dispose();
    }

    public void Receive(VideoPlayRequestMessage message)
    {
        _notificationService.HideToast();
    }
}
