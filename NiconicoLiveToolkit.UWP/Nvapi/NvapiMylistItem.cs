using NiconicoToolkit.Video;
using System;
using System.Text.Json.Serialization;

namespace NiconicoToolkit.Nvapi
{
    public class NvapiMylistItem
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("isPublic")]
        public bool IsPublic { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("defaultSortKey")]
        public string DefaultSortKey { get; set; }

        [JsonPropertyName("defaultSortOrder")]
        public string DefaultSortOrder { get; set; }

        [JsonPropertyName("itemsCount")]
        public long ItemsCount { get; set; }

        [JsonPropertyName("owner")]
        public Owner Owner { get; set; }

        [JsonPropertyName("sampleItems")]
        public SampleItem[] SampleItems { get; set; }

        [JsonPropertyName("followerCount")]
        public long FollowerCount { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTimeOffset CreatedAt { get; set; }

        [JsonPropertyName("isFollowing")]
        public bool IsFollowing { get; set; }
    }


    public partial class SampleItem
    {
        [JsonPropertyName("itemId")]
        public long ItemId { get; set; }

        [JsonPropertyName("watchId")]
        public string WatchId { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("addedAt")]
        public DateTimeOffset AddedAt { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("video")]
        public NvapiVideoItem Video { get; set; }
    }

}
