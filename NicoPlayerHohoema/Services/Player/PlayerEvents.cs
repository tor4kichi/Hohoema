using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Services.Player
{
    public struct PlayerPlayVideoRequestEventArgs
    {
        public string VideoId { get; set; }
    }

    public class PlayerPlayVideoRequest : PubSubEvent<PlayerPlayVideoRequestEventArgs>
    {
        
    }

    public struct PlayerPlayLiveRequestEventArgs
    {
        public string LiveId { get; set; }
    }


    public class PlayerPlayLiveRequest: PubSubEvent<PlayerPlayLiveRequestEventArgs>
    {
        
    }


}
