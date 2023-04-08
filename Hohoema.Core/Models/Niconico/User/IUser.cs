#nullable enable
using Hohoema.Models.Niconico.Follow;
using NiconicoToolkit.User;

namespace Hohoema.Models.Niconico;

public interface IUser : INiconicoObject, IFollowable
{
    UserId UserId { get; }
    string Nickname { get; }
    string IconUrl { get; }
}
