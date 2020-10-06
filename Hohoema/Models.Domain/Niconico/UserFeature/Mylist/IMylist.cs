using Mntone.Nico2.Users.Mylist;
using Hohoema.Models.Domain.Niconico.UserFeature.Follow;
using Hohoema.Models.Domain.Playlist;
using System;

namespace Hohoema.Models.Domain.Niconico.UserFeature.Mylist
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
