using NiconicoToolkit.Live;
using NiconicoToolkit.User;
using System;
using System.Text.Json.Serialization;

namespace NiconicoToolkit.Community
{
    public sealed class CommunityLiveResponse : ResponseWithMeta
    {
        [JsonPropertyName("data")]
        public CommunityLiveData Data { get; set; }

        public sealed class CommunityLiveData
        {
            [JsonPropertyName("total")]
            public long Total { get; set; }

            [JsonPropertyName("lives")]
            public CommunityLiveItem[] Lives { get; set; }
        }

        public sealed class CommunityLiveItem
        {
            [JsonPropertyName("id")]
            public LiveId Id { get; set; }

            [JsonPropertyName("title")]
            public string Title { get; set; }

            [JsonPropertyName("description")]
            public string Description { get; set; }

            [JsonPropertyName("status")]
            public CommunityLiveStatus Status { get; set; }

            [JsonPropertyName("user_id")]
            public UserId UserId { get; set; }

            [JsonPropertyName("watch_url")]
            public Uri WatchUrl { get; set; }

            [JsonPropertyName("features")]
            public CommunityLiveItemFeatures Features { get; set; }

            [JsonPropertyName("timeshift")]
            public CommunityLiveItemTimeshift Timeshift { get; set; }

            [JsonPropertyName("started_at")]
            [JsonConverter(typeof(CommunityApiDateTimeJsonConverter))]
            public DateTimeOffset StartedAt { get; set; }

            [JsonPropertyName("finished_at")]
            [JsonConverter(typeof(CommunityApiDateTimeJsonConverter))]
            public DateTimeOffset? FinishedAt { get; set; }
        }

        public sealed class CommunityLiveItemFeatures
        {
            [JsonPropertyName("is_member_only")]
            public bool IsMemberOnly { get; set; }
        }

        public sealed class CommunityLiveItemTimeshift
        {
            [JsonPropertyName("enabled")]
            public bool Enabled { get; set; }

            [JsonPropertyName("can_view")]
            public bool CanView { get; set; }

            [JsonPropertyName("finished_at")]
            [JsonConverter(typeof(CommunityApiDateTimeJsonConverter))]
            public DateTimeOffset FinishedAt { get; set; }
        }
    }

}
