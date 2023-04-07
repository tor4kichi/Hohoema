using Hohoema.Models.Niconico.Video;
using NiconicoToolkit.Channels;
using NiconicoToolkit.Video;
using System;

namespace Hohoema.Models.Niconico.Channel
{
    public sealed class ChannelVideoContent : IVideoContent, IVideoContentProvider
    {
        private readonly ChannelVideoItem _channelVideo;
        private readonly ChannelId _providerId;

        public ChannelVideoContent(ChannelVideoItem channelVideo, ChannelId providerId)
        {
            _channelVideo = channelVideo;
            _providerId = providerId;
        }
        public VideoId VideoId => _channelVideo.ItemId;

        public TimeSpan Length => _channelVideo.Length;

        public string ThumbnailUrl => _channelVideo.ThumbnailUrl;

        public DateTime PostedAt => _channelVideo.PostedAt;

        public string Title => _channelVideo.Title;

        public string ProviderId => _providerId.ToString();

        public OwnerType ProviderType => OwnerType.Channel;

        public bool Equals(IVideoContent other)
        {
            return this.VideoId == other?.VideoId;
        }
    }
}
