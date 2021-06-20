using NiconicoToolkit.Mylist;
using NiconicoToolkit.Video;
using System;

namespace NiconicoToolkit.Mylist
{
    public interface IMylistItem
    {
        DateTimeOffset CreatedAt { get; }
        MylistSortKey DefaultSortKey { get; }
        MylistSortOrder DefaultSortOrder { get; }
        string Description { get; }
        long FollowerCount { get; }
        MylistId Id { get; }
        bool IsFollowing { get; }
        bool IsPublic { get; }
        long ItemsCount { get; }
        string Name { get; }
        Owner Owner { get; }
        MylistItem[] SampleItems { get; }
    }
}