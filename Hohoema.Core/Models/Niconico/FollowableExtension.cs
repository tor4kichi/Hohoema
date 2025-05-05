#nullable enable
using Hohoema.Models.Niconico.Channel;
using Hohoema.Models.Niconico.Follow;
using Hohoema.Models.Niconico.Mylist;
using System;

namespace Hohoema.Models.Niconico;

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
            _ => throw new NotSupportedException(followable?.GetType().Name),
        };
    }
}
