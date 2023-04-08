using Hohoema.Models.Niconico.Video;
using NiconicoToolkit.Follow;

namespace Hohoema.ViewModels.Niconico.Follow;

public sealed class FollowTagViewModel : ITag
{
    public FollowTagViewModel(FollowTagsResponse.Tag tag)
    {
        _Tag = tag;
    }

    public string Tag => _Tag.Name;


    public FollowTagsResponse.Tag _Tag { get; }

    public string NicoDicSummary => _Tag.NicodicSummary;
}
