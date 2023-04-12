#nullable enable
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using NiconicoToolkit.Live;
using NiconicoToolkit.Video;
using System;

namespace Hohoema.Contracts.Player;

public readonly struct PlayerPlayLiveRequestEventArgs
{
    public PlayerPlayLiveRequestEventArgs(LiveId liveId)
    {
        LiveId = liveId;
    }

    public readonly LiveId LiveId;
}


public class PlayLiveRequestMessage : ValueChangedMessage<PlayerPlayLiveRequestEventArgs>
{
    public PlayLiveRequestMessage(PlayerPlayLiveRequestEventArgs value) : base(value)
    {
    }

    public PlayLiveRequestMessage(LiveId liveId) 
        : base(new (liveId))
    {

    }
}
