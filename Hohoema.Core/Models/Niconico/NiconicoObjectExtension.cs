using Hohoema.Models.Niconico.Channel;
using Hohoema.Models.Niconico.Community;
using Hohoema.Models.Niconico.Live;
using Hohoema.Models.Niconico.Mylist;
using Hohoema.Models.Niconico.Video;
using System;

namespace Hohoema.Models.Niconico;

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
