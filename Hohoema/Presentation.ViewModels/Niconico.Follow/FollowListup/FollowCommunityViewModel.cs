using Hohoema.Models.Domain.Niconico.Community;

namespace Hohoema.Presentation.ViewModels.Niconico.Follow
{
    public sealed class FollowCommunityViewModel : ICommunity
    {
        private readonly Mntone.Nico2.Users.Follow.FollowCommunityResponse.FollowCommunity _followCommunity;

        public string Id => _followCommunity.GlobalId;

        public string Label => _followCommunity.Name;

        public FollowCommunityViewModel(Mntone.Nico2.Users.Follow.FollowCommunityResponse.FollowCommunity followCommunity)
        {
            _followCommunity = followCommunity;
        }
    }
}
