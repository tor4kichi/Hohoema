using NiconicoToolkit.Video;
using System;
using System.Text.Json.Serialization;

namespace NiconicoToolkit.Mylist
{
    public sealed class GetMylistItemsResponse : ResponseWithMeta
    {
        [JsonPropertyName("data")]
        public GetMylistItemsData Data { get; set; }
    }


    public sealed class GetMylistItemsData
    {
        [JsonPropertyName("mylist")]
        public PagenatedNvapiMylistItem Mylist { get; set; }
    }

    public class PagenatedNvapiMylistItem : IMylistItem
    {
        [JsonPropertyName("hasInvisibleItems")]
        public bool HasInvisibleItems { get; set; }

        [JsonPropertyName("hasNext")]
        public bool HasNext { get; set; }

        [JsonPropertyName("totalItemCount")]
        public long TotalItemCount { get; set; }




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

        [JsonPropertyName("owner")]
        public Owner Owner { get; set; }

        [JsonPropertyName("items")]
        public MylistItem[] Items { get; set; }

        [JsonPropertyName("followerCount")]
        public long FollowerCount { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTimeOffset CreatedAt { get; set; }

        [JsonPropertyName("isFollowing")]
        public bool IsFollowing { get; set; }


        public long ItemsCount => TotalItemCount;

        public MylistItem[] SampleItems => Items;
    }
}
