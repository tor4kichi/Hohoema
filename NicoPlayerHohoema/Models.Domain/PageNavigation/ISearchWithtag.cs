using Hohoema.Models.Domain.Niconico.UserFeature.Follow;

namespace Hohoema.Models.Domain.PageNavigation
{
    public interface ISearchWithtag : IFollowable
    {
        string Tag { get; }
    }
}