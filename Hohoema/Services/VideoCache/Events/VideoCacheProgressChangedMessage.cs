using Hohoema.Models.VideoCache;
using CommunityToolkit.Mvvm.Messaging.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Services.VideoCache.Events
{
    public sealed class VideoCacheProgressChangedMessage : ValueChangedMessage<VideoCacheItem>
    {
        public VideoCacheProgressChangedMessage(VideoCacheItem value) : base(value)
        {
        }
    }
}
