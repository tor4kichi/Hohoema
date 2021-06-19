using Hohoema.Models.Domain.Niconico.Channel;
using Hohoema.Models.Domain.Niconico.Community;
using Hohoema.Models.Domain.Niconico.Follow;
using Hohoema.Models.Domain.Niconico.Mylist;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Domain.Niconico
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
