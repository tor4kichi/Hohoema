using Hohoema.Models.Domain.Niconico.Channel;
using Hohoema.Models.Domain.Niconico.Community;
using Hohoema.Models.Domain.Niconico.Live;
using Hohoema.Models.Domain.Niconico.Mylist;
using Hohoema.Models.Domain.Niconico.Video;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Domain.Niconico
{
    public static class NiconicoObjectExtension
    {
        public static string GetLabel(this INiconicoObject obj)
        {
            return obj switch
            {
                IVideoContent video => video.Title,
                ILiveContent live => live.Title,
                ICommunity community => community.Name,
                IChannel channel => channel.Name,
                IMylist mylist => mylist.Name,
                ITag tag => tag.Tag,
                IUser user => user.Nickname,
                _ => throw new NotSupportedException(obj.ToString())
            };
        }
    }
}
