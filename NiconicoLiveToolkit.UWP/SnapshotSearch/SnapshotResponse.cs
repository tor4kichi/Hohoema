using NiconicoToolkit.Channels;
using NiconicoToolkit.User;
using NiconicoToolkit.Video;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NiconicoToolkit.SnapshotSearch
{
    public sealed class SnapshotResponseMeta : Meta
    {
        [JsonPropertyName("id")]
        public Guid Id { get; init; }

        [JsonPropertyName("totalCount")]
        public long TotalCount { get; init; }

        [JsonPropertyName("errorMessage")]
        public string ErrorMessage { get; init; }
    }

    public sealed class SnapshotResponse : ResponseWithMeta<SnapshotResponseMeta>
    {
        [JsonPropertyName("data")]
        public SnapshotVideoItem[] Items { get; init; }
    }


    public sealed class SnapshotVideoItem
    {
        [JsonPropertyName("mylistCounter")]
        public long? MylistCounter { get; init; }

        [JsonPropertyName("lengthSeconds")]
        public long? LengthSeconds { get; init; }

        [JsonPropertyName("categoryTags")]
        public string CategoryTags { get; init; }

        [JsonPropertyName("viewCounter")]
        public long? ViewCounter { get; init; }

        [JsonPropertyName("commentCounter")]
        public long? CommentCounter { get; init; }

        [JsonPropertyName("likeCounter")]
        public long? LikeCounter { get; init; }

        [JsonPropertyName("genre")]
        public string Genre { get; init; }

        [JsonPropertyName("startTime")]
        public DateTimeOffset? StartTime { get; init; }

        [JsonPropertyName("lastCommentTime")]
        public DateTimeOffset? LastCommentTime { get; init; }

        [JsonPropertyName("description")]
        public string Description { get; init; }

        [JsonPropertyName("tags")]
        public string Tags { get; init; }

        [JsonPropertyName("lastResBody")]
        public string LastResBody { get; init; }

        [JsonPropertyName("contentId")]
        public VideoId? ContentId { get; init; }

        [JsonPropertyName("userId")]
        public UserId? UserId { get; init; }

        [JsonPropertyName("title")]
        public string Title { get; init; }

        [JsonPropertyName("channelId")]
        public ChannelId? ChannelId { get; init; }

        [JsonPropertyName("thumbnailUrl")]
        public Uri ThumbnailUrl { get; init; }
    }
}
