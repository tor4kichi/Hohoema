using NiconicoToolkit.Community;
using NiconicoToolkit.User;
using System;
using System.Collections.Generic;

namespace NiconicoToolkit.Follow
{
    public interface IFollowCommunity
    {
        DateTimeOffset CreateTime { get; }
        string Description { get; }
        string GlobalId { get; }
        CommunityId Id { get; }
        long Level { get; }
        string Name { get; }
        CommunityOptionFlags OptionFlags { get; }
        UserId OwnerId { get; }
        CommunityStatus Status { get; }
        List<CommunityTag> Tags { get; }
        long ThreadCount { get; }
        long ThreadMax { get; }
        CommunityThumbnailUrl ThumbnailUrl { get; }
        long UserCount { get; }
    }
}