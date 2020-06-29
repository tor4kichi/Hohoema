using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.UseCase.Events
{
    public sealed class VideoPlayedEvent : PubSubEvent<Events.VideoPlayedEvent.VideoPlayedEventArgs>
    {
        public sealed class VideoPlayedEventArgs
        {
            public string ContentId { get; set; }
        }
    }
}
