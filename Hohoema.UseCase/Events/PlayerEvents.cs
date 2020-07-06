using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.UseCase.Events
{
    public struct PlayerPlayVideoRequestEventArgs
    {
        public string VideoId { get; set; }
    }

    public class PlayerPlayVideoRequest : PubSubEvent<PlayerPlayVideoRequestEventArgs>
    {
        
    }
}
