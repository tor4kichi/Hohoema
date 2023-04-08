#nullable enable
using CommunityToolkit.Mvvm.Messaging;
using Hohoema.Models.Niconico.Live;
using Hohoema.Services.Player.Events;

namespace Hohoema.ViewModels.Niconico.Live;

public sealed class OpenLiveContentCommand : CommandBase
{
    private readonly IMessenger _messenger;

    public OpenLiveContentCommand(IMessenger messenger)
    {
        _messenger = messenger;
    }

    protected override bool CanExecute(object parameter)
    {
        return parameter is ILiveContent;
    }

    protected override void Execute(object parameter)
    {
        if (parameter is ILiveContent liveContent)
        {
            _messenger.Send(new PlayerPlayLiveRequestMessage(new () { LiveId = liveContent.LiveId }));
        }
    }
}
