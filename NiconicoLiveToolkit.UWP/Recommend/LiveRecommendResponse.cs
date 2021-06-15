using NiconicoToolkit.Live;
using NiconicoToolkit.User;
using System;
using System.Text.Json.Serialization;

namespace NiconicoToolkit.Recommend
{
    public sealed class LiveRecommendResponse : ResponseWithMeta
    {
        [JsonPropertyName("data")]
        public LiveRecommendData Data { get; set; }
    }
    public sealed class LiveRecommendData
    {
        [JsonPropertyName("recipe_id")]
        public string RecipeId { get; set; }

        [JsonPropertyName("recommend_id")]
        public string RecommendId { get; set; }

        [JsonPropertyName("values")]
        public LiveRecommendItem[] Items { get; set; }
    }

    public sealed class LiveRecommendItem
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("recommend_type")]
        public string RecommendType { get; set; }

        [JsonPropertyName("content_type")]
        public RecommendContentType ContentType { get; set; }

        [JsonPropertyName("content_meta")]
        public ContentMeta ContentMeta { get; set; }
    }

    public sealed class ContentMeta
    {
        [JsonPropertyName("community_text")]
        public string CommunityText { get; set; }

        [JsonPropertyName("timeshift_mode")]
        public long TimeshiftMode { get; set; }

        [JsonPropertyName("start_time")]
        public DateTimeOffset StartTime { get; set; }

        [JsonPropertyName("member_only")]
        public bool MemberOnly { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("tags")]
        public string Tags { get; set; }

        [JsonPropertyName("live_status")]
        public LiveStatus LiveStatus { get; set; }

        [JsonPropertyName("user_id")]
        public UserId? UserId { get; set; }

        [JsonPropertyName("provider_type")]
        public ProviderType ProviderType { get; set; }

        [JsonPropertyName("timeshift_expired")]
        public DateTimeOffset? TimeshiftExpired { get; set; }

        [JsonPropertyName("open_time")]
        public DateTimeOffset OpenTime { get; set; }

        [JsonPropertyName("live_end_time")]
        public DateTimeOffset LiveEndTime { get; set; }

        [JsonPropertyName("is_product_stream")]
        public bool IsProductStream { get; set; }

        [JsonPropertyName("channel_id")]
        public long? ChannelId { get; set; }

        [JsonPropertyName("community_id")]
        public long? CommunityId { get; set; }

        [JsonPropertyName("comment_counter")]
        public long CommentCounter { get; set; }

        [JsonPropertyName("timeshift_enabled")]
        public bool TimeshiftEnabled { get; set; }

        [JsonPropertyName("thumbnail_url")]
        public Uri ThumbnailUrl { get; set; }

        [JsonPropertyName("view_counter")]
        public long ViewCounter { get; set; }

        [JsonPropertyName("ss_adult")]
        public bool SsAdult { get; set; }

        [JsonPropertyName("category_tags")]
        public string CategoryTags { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("community_icon")]
        public Uri CommunityIcon { get; set; }

        [JsonPropertyName("picture_url")]
        public Uri PictureUrl { get; set; }

        [JsonPropertyName("score_timeshift_reserved")]
        public long ScoreTimeshiftReserved { get; set; }

        [JsonPropertyName("content_id")]
        public string ContentId { get; set; }

        [JsonPropertyName("live_screenshot_thumbnail_large")]
        public string LiveScreenshotThumbnailLarge { get; set; }

        [JsonPropertyName("live_screenshot_thumbnail_middle")]
        public string LiveScreenshotThumbnailMiddle { get; set; }

        [JsonPropertyName("live_screenshot_thumbnail_small")]
        public string LiveScreenshotThumbnailSmall { get; set; }

        [JsonPropertyName("live_screenshot_thumbnail_micro")]
        public string LiveScreenshotThumbnailMicro { get; set; }

        [JsonPropertyName("ts_screenshot_thumbnail_large")]
        public string TsScreenshotThumbnailLarge { get; set; }

        [JsonPropertyName("ts_screenshot_thumbnail_middle")]
        public string TsScreenshotThumbnailMiddle { get; set; }

        [JsonPropertyName("ts_screenshot_thumbnail_small")]
        public string TsScreenshotThumbnailSmall { get; set; }

        [JsonPropertyName("ts_screenshot_thumbnail_micro")]
        public string TsScreenshotThumbnailMicro { get; set; }

        [JsonPropertyName("thumbnail_huge_s1920x1080")]
        public Uri ThumbnailHugeS1920X1080 { get; set; }

        [JsonPropertyName("thumbnail_huge_s1280x720")]
        public Uri ThumbnailHugeS1280X720 { get; set; }

        [JsonPropertyName("thumbnail_huge_s640x360")]
        public Uri ThumbnailHugeS640X360 { get; set; }

        [JsonPropertyName("thumbnail_huge_s352x198")]
        public Uri ThumbnailHugeS352X198 { get; set; }

        [JsonPropertyName("user_nickname")]
        public string UserNickname { get; set; }

        [JsonPropertyName("user_icon_150x150")]
        public Uri UserIcon150X150 { get; set; }

        [JsonPropertyName("user_icon_50x50")]
        public Uri UserIcon50X50 { get; set; }
    }


}