using NiconicoToolkit.Mylist;
using NiconicoToolkit.Video;
using System;
using System.Text.Json.Serialization;

namespace NiconicoToolkit.Mylist
{
    public sealed class NvapiMylistItem : IMylistItem
    {
        [JsonPropertyName("id")]
        public MylistId Id { get; set; }

        [JsonPropertyName("isPublic")]
        public bool IsPublic { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("defaultSortKey")]
        public MylistSortKey DefaultSortKey { get; set; }

        [JsonPropertyName("defaultSortOrder")]
        public MylistSortOrder DefaultSortOrder { get; set; }

        [JsonPropertyName("itemsCount")]
        public long ItemsCount { get; set; }

        [JsonPropertyName("owner")]
        public Owner Owner { get; set; }

        [JsonPropertyName("sampleItems")]
        public MylistItem[] SampleItems { get; set; }

        [JsonPropertyName("followerCount")]
        public long FollowerCount { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTimeOffset CreatedAt { get; set; }

        [JsonPropertyName("isFollowing")]
        public bool IsFollowing { get; set; }
    }


    public sealed class MylistItem
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

        public bool IsDeleted => Status == "deleted";
    }

}
