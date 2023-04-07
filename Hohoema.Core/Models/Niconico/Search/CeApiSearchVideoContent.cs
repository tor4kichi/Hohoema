using Hohoema.Models.Niconico.Video;
using NiconicoToolkit.SearchWithCeApi.Video;
using NiconicoToolkit.Video;
using System;

namespace Hohoema.Models.Niconico.Search
{
    public sealed class CeApiSearchVideoContent : IVideoContent, IVideoContentProvider
    {
        private readonly VideoItem _videoItem;

        public CeApiSearchVideoContent(VideoItem videoItem)
        {
            _videoItem = videoItem;
            ProviderId = _videoItem.ProviderType == VideoProviderType.Regular ? _videoItem.UserId : _videoItem.CommunityId;
            ProviderType = _videoItem.ProviderType == VideoProviderType.Regular ? OwnerType.User : OwnerType.Channel;
        }
        public VideoId VideoId => _videoItem.Id;

        public TimeSpan Length => TimeSpan.FromSeconds(_videoItem.LengthInSeconds);

        public string ThumbnailUrl => _videoItem.ThumbnailUrl.OriginalString;

        public DateTime PostedAt => _videoItem.FirstRetrieve.DateTime;

        public string Title => _videoItem.Title;

        public bool Equals(IVideoContent other)
        {
            return this.VideoId == other.VideoId;
        }

        public string ProviderId { get; }

        public OwnerType ProviderType { get; }

    }
}
