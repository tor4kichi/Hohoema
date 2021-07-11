using Hohoema.Models.Domain.Niconico.Follow.LoginUser;
using Hohoema.Models.Domain.Playlist;
using System;
using Hohoema.Models.Domain.Niconico.Follow;
using NiconicoToolkit.Mylist;
using NiconicoToolkit.User;
using NiconicoToolkit;

namespace Hohoema.Models.Domain.Niconico.Mylist
{
    public interface IMylist : IFollowable, INiconicoObject
    {
        PlaylistId PlaylistId { get; }
        string Name { get; }
        MylistId MylistId { get; }
        string Description { get; }
        string UserId { get; }
        bool IsPublic { get; }
        MylistSortOrder DefaultSortOrder { get; }
        MylistSortKey DefaultSortKey { get; }
        DateTime CreateTime { get; }
    }
}
