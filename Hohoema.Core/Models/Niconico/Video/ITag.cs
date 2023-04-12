#nullable enable
using Hohoema.Models.Niconico.Follow;

namespace Hohoema.Models.Niconico.Video;

public interface ITag : IFollowable
{
    string Tag { get; }
}
