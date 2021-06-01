using Hohoema.Models.Domain.Niconico.Follow.LoginUser;
using Hohoema.Models.Domain.Playlist;
using System;
using Hohoema.Models.Domain.Niconico.Follow;
using NiconicoToolkit.Mylist;

namespace Hohoema.Models.Domain.Niconico.Mylist
{
    public interface IMylist : IPlaylist, IFollowable
    {
        new string Label { get; }
        new string Id { get; }
        string Description { get; }
        string UserId { get; }
        bool IsPublic { get; }
        MylistSortOrder DefaultSortOrder { get; }
        MylistSortKey DefaultSortKey { get; }
        DateTime CreateTime { get; }
    }
}
