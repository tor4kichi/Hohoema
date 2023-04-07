using Hohoema.Models.Niconico.Channel;
using Hohoema.Models.Niconico.Community;
using Hohoema.Models.Niconico.Follow;
using Hohoema.Models.Niconico.Mylist;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Niconico
{
    public static class FollowableExtension
    {
        public static string GetLabel(this IFollowable followable)
        {
            return followable switch
            {
                IUser user => user.Nickname,
                Video.ITag tag => tag.Tag,
                IChannel channel => channel.Name,
                IMylist mylist => mylist.Name,
                ICommunity community => community.Name,
                _ => throw new NotSupportedException(followable?.GetType().Name),
            };
        }
    }
}
