using Microsoft.Toolkit.Mvvm.Messaging.Messages;
using NiconicoToolkit.Video;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.UseCase.Niconico.Player.Events
{
    public sealed class VideoPlayedMessage : ValueChangedMessage<Events.VideoPlayedMessage.VideoPlayedEventArgs>
    {
        public VideoPlayedMessage(VideoPlayedEventArgs value) : base(value)
        {
        }

        public sealed class VideoPlayedEventArgs
        {
            public VideoId ContentId { get; set; }
            public TimeSpan PlayedPosition { get; set; }
        }
    }
}
