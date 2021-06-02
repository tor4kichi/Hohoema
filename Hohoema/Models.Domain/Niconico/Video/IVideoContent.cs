using Hohoema.Presentation.Services;
using NiconicoToolkit.Video;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Domain.Niconico.Video
{
    public interface IVideoContent : INiconicoContent, IEquatable<IVideoContent>
    {
        TimeSpan Length { get; }
        string ThumbnailUrl { get; }
    }

    public interface IVideoContentProvider
    {
        string ProviderId { get; }
        OwnerType ProviderType { get; }
    }

    public interface IVideoDetail : IVideoContent, IVideoContentProvider
    {
        string VideoId { get; }

        int ViewCount { get; }
        int MylistCount { get; }
        int CommentCount { get; }

        string Description { get; }
        DateTime PostedAt { get; }
        bool IsDeleted { get; }
        VideoPermission Permission { get; }
    }

    public interface IVideoDetailWritable : IVideoDetail
    {
        new string ProviderId { get; set; }
        new OwnerType ProviderType { get; set; }

        new string Label { get; set; }
        new TimeSpan Length { get; set; }
        new DateTime PostedAt { get; set; }

        new int ViewCount { get; set; }
        new int MylistCount { get; set; }
        new int CommentCount { get; set; }

        new string ThumbnailUrl { get; set; }

        new string Description { get; set; }
        new bool IsDeleted { get; set; }

        new VideoPermission Permission { get; set; }
    }
}
