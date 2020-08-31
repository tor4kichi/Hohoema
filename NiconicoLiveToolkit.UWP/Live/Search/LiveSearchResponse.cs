using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json.Serialization;

namespace NiconicoLiveToolkit.Live.Search
{
    public class LiveSearchResultItem
    {
        [JsonPropertyName("communityId")]
        public int? CommunityId { get; set; }

        [JsonPropertyName("openTime")]
        public DateTime? OpenTime { get; set; }

        [JsonPropertyName("startTime")]
        public DateTime? StartTime { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("communityIcon")]
        public string CommunityIcon { get; set; }

        [JsonPropertyName("tags")]
        public string Tags { get; set; }

        [JsonPropertyName("liveEndTime")]
        public DateTime? LiveEndTime { get; set; }

        [JsonPropertyName("timeshiftEnabled")]
        public bool? TimeshiftEnabled { get; set; }

        [JsonPropertyName("categoryTags")]
        public string CategoryTags { get; set; }

        [JsonPropertyName("viewCounter")]
        public int? ViewCounter { get; set; }

        [JsonPropertyName("providerType")]
        [JsonConverter(typeof(JsonStringEnumMemberConverter))]
        public ProviderType ProviderType { get; set; }

        [JsonPropertyName("contentId")]
        public string ContentId { get; set; }

        [JsonPropertyName("userId")]
        public int? UserId { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("memberOnly")]
        public bool? MemberOnly { get; set; }

        [JsonPropertyName("scoreTimeshiftReserved")]
        public int? ScoreTimeshiftReserved { get; set; }

        [JsonPropertyName("commentCounter")]
        public int? CommentCounter { get; set; }

        [JsonPropertyName("communityText")]
        public string CommunityText { get; set; }

        [JsonPropertyName("channelId")]
        public int? ChannelId { get; set; }

        [JsonPropertyName("thumbnailUrl")]
        public string ThumbnailUrl { get; set; }

        [JsonPropertyName("liveStatus")]
        [JsonConverter(typeof(JsonStringEnumMemberConverter))]
        public LiveStatus? LiveStatus { get; set; }

    }

    
    public class Meta
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("totalCount")]
        public int? TotalCount { get; set; }

        [JsonPropertyName("status")]
        public int Status { get; set; }

        [JsonPropertyName("errorCode")]
        public string ErrorCode { get; set; }

        [JsonPropertyName("errorMessage")]
        public string ErrorMessage { get; set; }
    }

    
    public class LiveSearchResponse
    {
        [JsonPropertyName("data")]
        public LiveSearchResultItem[] Data { get; set; }

        [JsonPropertyName("meta")]
        public Meta Meta { get; set; }


        public bool IsOK => Meta?.Status == 200;
        public bool IsQueryParseError => Meta?.Status == 400;
        public bool IsIsInternalServerError => Meta?.Status == 500;
        public bool IsMaintenance => Meta?.Status == 503;
    }




}
