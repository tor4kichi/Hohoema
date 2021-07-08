using Hohoema.Models.Domain.Niconico.Video;
using NiconicoToolkit.User;
using NiconicoToolkit.Video;
using System;

namespace Hohoema.Models.Domain.Video
{
    public class NvapiVideoContent : IVideoContent, IVideoContentProvider
    {
        private readonly NvapiVideoItem _videoItem;

        public NvapiVideoContent(NvapiVideoItem videoItem)
        {
            _videoItem = videoItem;
        }
        public VideoId VideoId => _videoItem.Id;

        public TimeSpan Length => TimeSpan.FromSeconds(_videoItem.Duration);

        public string ThumbnailUrl => _videoItem.Thumbnail.Url.OriginalString;

        public DateTime PostedAt => _videoItem.RegisteredAt.DateTime;

        public string Title => _videoItem.Title;

        public string ProviderId => _videoItem.Owner.Id;

        public OwnerType ProviderType => OwnerType.User;

        public bool Equals(IVideoContent other)
        {
            return this.VideoId == other.VideoId;
        }
    }
}
