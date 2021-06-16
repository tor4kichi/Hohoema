using Hohoema.Models.Domain.Niconico.Follow;
using NiconicoToolkit.User;

namespace Hohoema.Models.Domain.Niconico
{
    public interface IUser : INiconicoObject, IFollowable
    {
        UserId UserId { get; }
        string Nickname { get; }
        string IconUrl { get; }
    }
}
