#nullable enable
using CommunityToolkit.Mvvm.Messaging;
using Hohoema.Models.Playlist;
using I18NPortable;

namespace Hohoema.Services.Playlist;

public sealed class AutoSkipToPlaylistNextVideoWhenPlayFailed 
    : IRecipient<PlaybackFailedMessage>
{
    private readonly IMessenger _messenger;
    private readonly INotificationService _notificationService;

    public AutoSkipToPlaylistNextVideoWhenPlayFailed(
        IMessenger messenger,
        INotificationService notificationService
        )
    {
        _messenger = messenger;
        _notificationService = notificationService;
        _messenger.Register(this);
    }

    public async void Receive(PlaybackFailedMessage message)
    {
        var player = message.Value.Player;

        if (await player.CanGoNextAsync())
        {
            _notificationService.ShowLiteInAppNotification_Fail($"{"CanNotPlay".Translate()}\n{message.Value.FailedReason.Translate()}");
            await player.GoNextAsync();
        }
        else
        {
            _notificationService.ShowLiteInAppNotification_Fail($"{"CanNotPlay".Translate()}\n{message.Value.FailedReason.Translate()}");
        }
    }
}
