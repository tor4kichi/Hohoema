using NiconicoToolkit.Channels;
using System;
using System.Text.Json.Serialization;
#if WINDOWS_UWP
#else
using System.Net;
using System.Net.Http;
#endif

namespace NiconicoToolkit.Follow
{
    public sealed class ChannelAuthorityResponse : ResponseWithMeta
    {
        [JsonPropertyName("data")]
        public ChannelAuthorityData Data { get; set; }
    }



    public sealed class ChannelAuthorityData
    {
        [JsonPropertyName("session")]
        public Session Session { get; set; }

        [JsonPropertyName("hasVideo")]
        public bool HasVideo { get; set; }

        [JsonPropertyName("hasLive")]
        public bool HasLive { get; set; }

        [JsonPropertyName("hasOfficialLive")]
        public bool HasOfficialLive { get; set; }

        [JsonPropertyName("hasBlog")]
        public bool HasBlog { get; set; }

        [JsonPropertyName("hasEvent")]
        public bool HasEvent { get; set; }

        [JsonPropertyName("hasTwitter")]
        public bool HasTwitter { get; set; }

        [JsonPropertyName("hasYoutube")]
        public bool HasYoutube { get; set; }

        [JsonPropertyName("hasRss")]
        public bool HasRss { get; set; }

        [JsonPropertyName("hasSpecialContent")]
        public bool HasSpecialContent { get; set; }

        [JsonPropertyName("defaultContent")]
        public string DefaultContent { get; set; }

        [JsonPropertyName("channel")]
        public Channel Channel { get; set; }

        [JsonPropertyName("video")]
        public OfficialLive Video { get; set; }

        [JsonPropertyName("officialLive")]
        public OfficialLive OfficialLive { get; set; }
    }

    public partial class Channel : IChannelItem
    {
        [JsonPropertyName("id")]
        public ChannelId Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("descriptionHtml")]
        public string DescriptionHtml { get; set; }

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

        [JsonPropertyName("lastPublishedAt")]
        public DateTimeOffset LastPublishedAt { get; set; }

        [JsonPropertyName("backgroundImage")]
        public BackgroundImage BackgroundImage { get; set; }

        [JsonPropertyName("rss")]
        public object[] Rss { get; set; }

        [JsonPropertyName("officialLiveTags")]
        public OfficialLiveTag[] OfficialLiveTags { get; set; }
    }

    public partial class BackgroundImage
    {
        [JsonPropertyName("url")]
        public Uri Url { get; set; }

        [JsonPropertyName("repeatFlag")]
        public long RepeatFlag { get; set; }
    }

    public partial class OfficialLiveTag
    {
        [JsonPropertyName("text")]
        public string Text { get; set; }
    }

    public partial class OfficialLive
    {
        [JsonPropertyName("lastPublishedAt")]
        public DateTimeOffset LastPublishedAt { get; set; }
    }

    public class Session
    {
        [JsonPropertyName("hasContentsAuthority")]
        public bool HasContentsAuthority { get; set; }

        [JsonPropertyName("isJoining")]
        public bool IsJoining { get; set; }

        [JsonPropertyName("isFollowing")]
        public bool IsFollowing { get; set; }

        [JsonPropertyName("subscribingTopics")]
        public object[] SubscribingTopics { get; set; }
    }
}
