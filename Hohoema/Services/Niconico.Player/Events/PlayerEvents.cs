using CommunityToolkit.Mvvm.Messaging.Messages;
using NiconicoToolkit.Live;
using NiconicoToolkit.Video;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Services.Niconico.Player.Events
{
    public struct PlayerPlayVideoRequestEventArgs
    {
        public VideoId VideoId { get; set; }
        public TimeSpan Position { get; set; }
    }

    public class PlayerPlayVideoRequestMessage : ValueChangedMessage<PlayerPlayVideoRequestEventArgs>
    {
        public PlayerPlayVideoRequestMessage(PlayerPlayVideoRequestEventArgs value) : base(value)
        {
        }
    }

    public struct PlayerPlayLiveRequestEventArgs
    {
        public LiveId LiveId { get; set; }
    }


    public class PlayerPlayLiveRequestMessage : ValueChangedMessage<PlayerPlayLiveRequestEventArgs>
    {
        public PlayerPlayLiveRequestMessage(PlayerPlayLiveRequestEventArgs value) : base(value)
        {
        }
    }


}
