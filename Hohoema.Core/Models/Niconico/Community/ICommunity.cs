#nullable enable
using Hohoema.Models.Niconico.Follow;
using NiconicoToolkit.Community;

namespace Hohoema.Models.Niconico.Community;

public interface ICommunity : INiconicoGroup, IFollowable
{
    public CommunityId CommunityId { get; }
}
