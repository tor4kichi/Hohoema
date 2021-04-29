using Hohoema.Models.Domain.Niconico.Follow;
using Hohoema.Models.Domain.Niconico.Follow.LoginUser;

namespace Hohoema.Models.Domain.PageNavigation
{
    public interface ISearchWithtag : IFollowable
    {
        string Tag { get; }
    }
}