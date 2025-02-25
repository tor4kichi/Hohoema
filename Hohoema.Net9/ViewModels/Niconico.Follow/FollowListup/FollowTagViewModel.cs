#nullable enable
using Hohoema.Models.Niconico.Video;
using NiconicoToolkit.Follow;

namespace Hohoema.ViewModels.Niconico.Follow;

public sealed class FollowTagViewModel : ITag
{
    public FollowTagViewModel(FollowTag tag)
    {
        _Tag = tag;
    }

    public string Tag => _Tag.Name;


    public FollowTag _Tag { get; }

    public string NicoDicSummary => _Tag.NicodicSummary;
}
