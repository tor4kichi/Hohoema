#nullable enable
using Hohoema.Models.Niconico.Follow;
using Hohoema.Models.Playlist;
using NiconicoToolkit.Mylist;
using System;

namespace Hohoema.Models.Niconico.Mylist;

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
