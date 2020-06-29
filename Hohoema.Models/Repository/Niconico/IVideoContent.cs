using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Repository
{
    public interface IVideoContent : INiconicoContent, IEquatable<IVideoContent>
    {
        string ProviderId { get; }
        Database.NicoVideoUserType ProviderType { get; }

        TimeSpan Length { get; }
        DateTime PostedAt { get; }

        int ViewCount { get; }
        int MylistCount { get; }
        int CommentCount { get; }

        string ThumbnailUrl { get; }

        string Description { get; }
        bool IsDeleted { get; }
    }

    public interface IVideoContentWritable : IVideoContent
    {
        new string ProviderId { get; set; }
        new Database.NicoVideoUserType ProviderType { get; set; }

        new string Label { get; set; }
        new TimeSpan Length { get; set; }
        new DateTime PostedAt { get; set; }

        new int ViewCount { get; set; }
        new int MylistCount { get; set; }
        new int CommentCount { get; set; }

        new string ThumbnailUrl { get; set; }

        new string Description { get; set; }
        new bool IsDeleted { get; set; }
    }
}
