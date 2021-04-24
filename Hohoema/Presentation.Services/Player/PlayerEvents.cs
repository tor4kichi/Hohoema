using Microsoft.Toolkit.Mvvm.Messaging.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Presentation.Services.Player
{
    public struct PlayerPlayVideoRequestEventArgs
    {
        public string VideoId { get; set; }
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
        public string LiveId { get; set; }
    }


    public class PlayerPlayLiveRequestMessage : ValueChangedMessage<PlayerPlayLiveRequestEventArgs>
    {
        public PlayerPlayLiveRequestMessage(PlayerPlayLiveRequestEventArgs value) : base(value)
        {
        }
    }


}
