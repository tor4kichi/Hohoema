using Hohoema.Models.Domain.Niconico.LoginUser.Follow;

namespace Hohoema.Models.Domain.PageNavigation
{
    public interface ISearchWithtag : IFollowable
    {
        string Tag { get; }
    }
}