using NiconicoToolkit.Channels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NiconicoToolkit.Follow
{
    public sealed class FollowChannelResponse : ResponseWithMeta<FollowChannelMeta>
    {
        [JsonPropertyName("data")]
        public ChannelItem[] Data { get; set; }
    }

    public class ChannelItem : IChannelItem
    {
        [JsonPropertyName("session")]
        public SessionChannelInfo Session { get; set; }

        [JsonPropertyName("id")]
        public ChannelId Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("isFree")]
        public bool IsFree { get; set; }

        [JsonPropertyName("screenName")]
        public string ScreenName { get; set; }

        [JsonPropertyName("ownerName")]
        public string OwnerName { get; set; }

        [JsonPropertyName("price")]
        public long Price { get; set; }

        [JsonPropertyName("bodyPrice")]
        public long BodyPrice { get; set; }

        [JsonPropertyName("url")]
        public Uri Url { get; set; }

        [JsonPropertyName("thumbnailUrl")]
        public Uri ThumbnailUrl { get; set; }

        [JsonPropertyName("thumbnailSmallUrl")]
        public Uri ThumbnailSmallUrl { get; set; }

        [JsonPropertyName("canAdmit")]
        public bool CanAdmit { get; set; }

        [JsonPropertyName("isAdult")]
        public bool IsAdult { get; set; }
    }

    public class SessionChannelInfo
    {
        [JsonPropertyName("joining")]
        public bool Joining { get; set; }
    }

    public class FollowChannelMeta : Meta
    {
        [JsonPropertyName("total")]
        public long Total { get; set; }

        [JsonPropertyName("count")]
        public long Count { get; set; }
    }
}
