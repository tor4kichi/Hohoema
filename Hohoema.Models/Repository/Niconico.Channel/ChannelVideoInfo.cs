using System;

namespace Hohoema.Models.Repository.Niconico.Channel
{
    public sealed class ChannelVideoInfo
    {
        private readonly Mntone.Nico2.Channels.Video.ChannelVideoInfo _info;

        public ChannelVideoInfo(Mntone.Nico2.Channels.Video.ChannelVideoInfo info)
        {
            _info = info;
        }

        public string ItemId => _info.ItemId;

        public string Title => _info.Title;
        public string ThumbnailUrl => _info.ThumbnailUrl;
        public TimeSpan Length => _info.Length;

        public int ViewCount => _info.ViewCount;
        public int CommentCount => _info.CommentCount;
        public int MylistCount => _info.MylistCount;

        public DateTime PostedAt => _info.PostedAt;

        public string Description => _info.Description;

        public string CommentSummary => _info.CommentSummary;

        public bool IsRequirePayment => _info.IsRequirePayment;
        public string PurchasePreviewUrl => _info.PurchasePreviewUrl;
    }
}
