using Hohoema.Models.Domain.Niconico.Follow;
using NiconicoToolkit;

namespace Hohoema.Models.Domain.Niconico.Community
{
    public interface ICommunity : INiconicoGroup, IFollowable
    {
        public NiconicoId CommunityId { get; }
    }
}
