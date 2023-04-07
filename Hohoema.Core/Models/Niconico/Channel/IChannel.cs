using Hohoema.Models.Niconico.Follow;
using Hohoema.Models.Niconico.Follow.LoginUser;
using NiconicoToolkit;
using NiconicoToolkit.Channels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Niconico.Channel
{
    public interface IChannel : INiconicoGroup, IFollowable
    {
        public ChannelId ChannelId { get; }
        public new string Name { get; }
    }
}
