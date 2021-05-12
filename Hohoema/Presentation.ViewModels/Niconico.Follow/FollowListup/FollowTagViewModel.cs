using Hohoema.Models.Domain.Niconico.Video;
using Mntone.Nico2.Users.Follow;

namespace Hohoema.Presentation.ViewModels.Niconico.Follow
{
    public sealed class FollowTagViewModel : ITag
    {
        public FollowTagViewModel(FollowTagsResponse.Tag tag)
        {
            _Tag = tag;
        }

        public string Tag => _Tag.Name;

        public string Id => _Tag.Name;

        public string Label => _Tag.Name;

        public FollowTagsResponse.Tag _Tag { get; }

        public string NicoDicSummary => _Tag.NicodicSummary;
    }
}
