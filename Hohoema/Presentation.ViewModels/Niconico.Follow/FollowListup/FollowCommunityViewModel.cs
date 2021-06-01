using Hohoema.Models.Domain.Niconico.Community;
using NiconicoToolkit.Follow;

namespace Hohoema.Presentation.ViewModels.Niconico.Follow
{
    public sealed class FollowCommunityViewModel : ICommunity
    {
        public bool IsOwnedCommunity { get; }

        private readonly IFollowCommunity _followCommunity;

        public string Id => _followCommunity.GlobalId;

        public string Label => _followCommunity.Name;

        public string Description => _followCommunity.Description;

        public string ThumbnailUrl => _followCommunity.ThumbnailUrl.Small.OriginalString;

        public FollowCommunityViewModel(IFollowCommunity followCommunity, bool isOwnedCommnity)
        {
            _followCommunity = followCommunity;
            IsOwnedCommunity = isOwnedCommnity;
        }
    }
}
