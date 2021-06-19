using Hohoema.Models.Domain.Niconico.Follow;
using NiconicoToolkit;
using NiconicoToolkit.Community;

namespace Hohoema.Models.Domain.Niconico.Community
{
    public interface ICommunity : INiconicoGroup, IFollowable
    {
        public CommunityId CommunityId { get; }
    }
}
