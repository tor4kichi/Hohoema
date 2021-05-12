using Hohoema.Models.Domain.Niconico.Follow;

namespace Hohoema.Models.Domain.Niconico.Video
{
    public interface ITag : IFollowable
    {
        string Tag { get; }
    }
}
