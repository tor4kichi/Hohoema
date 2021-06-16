using System;
using System.Text.Json.Serialization;

namespace NiconicoToolkit.Community
{
    public sealed class CommunityVideoResponse : ResponseWithMeta
    {
        [JsonPropertyName("data")]
        public CommunityVideoData Data { get; set; }

        public sealed class CommunityVideoData
        {
            [JsonPropertyName("total")]
            public long Total { get; set; }

            [JsonPropertyName("max")]
            public long Max { get; set; }

            [JsonPropertyName("contents")]
            public CommunityContent[] Contents { get; set; }
        }

        public sealed class CommunityContent
        {
            [JsonPropertyName("id")]
            public long Id { get; set; }

            [JsonPropertyName("community_id")]
            public long CommunityId { get; set; }

            [JsonPropertyName("content_kind")]
            public CommunityContentKind ContentKind { get; set; }

            [JsonPropertyName("content_id")]
            public NiconicoId ContentId { get; set; }

            [JsonPropertyName("original")]
            public long Original { get; set; }

            [JsonPropertyName("can_be_deleted")]
            public bool CanBeDeleted { get; set; }

            [JsonPropertyName("cached_view_count")]
            public long CachedViewCount { get; set; }

            [JsonPropertyName("cached_comment_count")]
            public long CachedCommentCount { get; set; }

            [JsonPropertyName("cached_mylist_count")]
            public long CachedMylistCount { get; set; }

            [JsonPropertyName("cached_content_length")]
            public long CachedContentLength { get; set; }

            [JsonPropertyName("cached_last_comment_time")]

            [JsonConverter(typeof(CommunityApiDateTimeJsonConverter))]
            public DateTimeOffset CachedLastCommentTime { get; set; }

            [JsonPropertyName("cached_post_time")]
            [JsonConverter(typeof(CommunityApiDateTimeJsonConverter))]
            public DateTimeOffset CachedPostTime { get; set; }

            [JsonPropertyName("cache_expire_time")]
            [JsonConverter(typeof(CommunityApiDateTimeJsonConverter))]
            public DateTimeOffset CacheExpireTime { get; set; }

            [JsonPropertyName("create_time")]
            [JsonConverter(typeof(CommunityApiDateTimeJsonConverter))]
            public DateTimeOffset CreateTime { get; set; }

            [JsonPropertyName("update_time")]
            [JsonConverter(typeof(CommunityApiDateTimeJsonConverter))]
            public DateTimeOffset UpdateTime { get; set; }
        }
    }

    
    public enum CommunityContentKind
    {
        Video,
    }
}
