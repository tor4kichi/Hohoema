using System;
using System.Text.Json.Serialization;


namespace NiconicoToolkit.Video
{
    public class NvapiVideoItem
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("id")]
        public VideoId Id { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("registeredAt")]
        public DateTimeOffset RegisteredAt { get; set; }

        [JsonPropertyName("count")]
        public Count Count { get; set; }

        [JsonPropertyName("thumbnail")]
        public Thumbnail Thumbnail { get; set; }

        [JsonPropertyName("duration")]
        public long Duration { get; set; }

        [JsonPropertyName("shortDescription")]
        public string ShortDescription { get; set; }

        [JsonPropertyName("latestCommentSummary")]
        public string LatestCommentSummary { get; set; }

        [JsonPropertyName("isChannelVideo")]
        public bool IsChannelVideo { get; set; }

        [JsonPropertyName("isPaymentRequired")]
        public bool IsPaymentRequired { get; set; }

        [JsonPropertyName("playbackPosition")]
        public long? PlaybackPosition { get; set; }

        [JsonPropertyName("owner")]
        public Owner Owner { get; set; }

        [JsonPropertyName("requireSensitiveMasking")]
        public bool RequireSensitiveMasking { get; set; }

        [JsonPropertyName("9d091f87")]
        public bool The9D091F87 { get; set; }

        [JsonPropertyName("acf68865")]
        public bool Acf68865 { get; set; }


        public bool IsDeleted => Duration == 0;
    }

    public enum TypeEnum { Essential };

    public enum OwnerType { User, Channel, Hidden };

    public class Count
    {
        [JsonPropertyName("view")]
        public int View { get; set; }

        [JsonPropertyName("comment")]
        public int Comment { get; set; }

        [JsonPropertyName("mylist")]
        public int Mylist { get; set; }

        [JsonPropertyName("like")]
        public int Like { get; set; }
    }

    public class Owner
    {
        [JsonPropertyName("ownerType")]
        public OwnerType OwnerType { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("iconUrl")]
        public Uri IconUrl { get; set; }
    }

    public class Thumbnail
    {
        [JsonPropertyName("url")]
        public Uri Url { get; set; }

        [JsonPropertyName("middleUrl")]
        public Uri MiddleUrl { get; set; }

        [JsonPropertyName("largeUrl")]
        public Uri LargeUrl { get; set; }

        [JsonPropertyName("listingUrl")]
        public Uri ListingUrl { get; set; }

        [JsonPropertyName("nHdUrl")]
        public Uri NHdUrl { get; set; }
    }

}
