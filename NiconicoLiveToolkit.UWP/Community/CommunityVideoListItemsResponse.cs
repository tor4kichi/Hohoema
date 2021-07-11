using NiconicoToolkit.User;
using NiconicoToolkit.Video;
using System;
using System.Text.Json.Serialization;

namespace NiconicoToolkit.Community
{
    public sealed class CommunityVideoListItemsResponse : ResponseWithMeta
    {
        [JsonPropertyName("data")]
        public CommunityVideoListItemsData Data { get; set; }

        public sealed class CommunityVideoListItemsData
        {
            [JsonPropertyName("videos")]
            public CommunityVideoListItem[] Videos { get; set; }
        }

        public sealed class CommunityVideoListItem
        {
            [JsonPropertyName("id")]
            public VideoId Id { get; set; }

            [JsonPropertyName("member_only")]
            public bool MemberOnly { get; set; }

            [JsonPropertyName("title")]
            public string Title { get; set; }

            [JsonPropertyName("description")]
            public string Description { get; set; }

            [JsonPropertyName("thumbnail_url")]
            public Uri ThumbnailUrl { get; set; }

            [JsonPropertyName("latest_comments")]
            public string[] LatestComments { get; set; }

            [JsonPropertyName("content_length")]
            public long ContentLength { get; set; }

            [JsonPropertyName("user_id")]
            public UserId UserId { get; set; }

            [JsonPropertyName("create_time")]
            public string CreateTime { get; set; }

            [JsonPropertyName("deleted_reason")]
            public string DeletedReason { get; set; }


            public DateTime GetCreateTime()
            {
                return DateTimeOffset.Parse(CreateTime).DateTime;
            }
        }
    }

   
}
