using Hohoema.Models.Domain.Niconico.Community;

namespace Hohoema.Presentation.ViewModels.Niconico.Follow
{
    public sealed class FollowCommunityViewModel : ICommunity
    {
        public bool IsOwnedCommunity { get; }

        private readonly Mntone.Nico2.Users.Follow.FollowCommunityResponse.FollowCommunity _followCommunity;

        public string Id => _followCommunity.GlobalId;

        public string Label => _followCommunity.Name;

        public string Description => _followCommunity.Description;

        public string ThumbnailUrl => _followCommunity.ThumbnailUrl.Small.OriginalString;

        public FollowCommunityViewModel(Mntone.Nico2.Users.Follow.FollowCommunityResponse.FollowCommunity followCommunity, bool isOwnedCommnity)
        {
            _followCommunity = followCommunity;
            IsOwnedCommunity = isOwnedCommnity;
        }
    }
}
