#nullable enable
using NiconicoToolkit.Video;
using System;

namespace Hohoema.Models.Niconico.Video;

public interface IVideoContent : INiconicoContent, IEquatable<IVideoContent>
{
    //string Title { get; }
    VideoId VideoId { get; }
    TimeSpan Length { get; }
    string ThumbnailUrl { get; }
    DateTime PostedAt { get; }
}

public interface IVideoContentProvider
{
    string ProviderId { get; }
    OwnerType ProviderType { get; }
}

public interface IVideoDetail : IVideoContent, IVideoContentProvider
{
    int ViewCount { get; }
    int MylistCount { get; }
    int CommentCount { get; }

    string Description { get; }
    bool IsDeleted { get; }
    VideoPermission Permission { get; }

    string ProviderName { get; }
    string ProviderIconUrl { get; }
}
