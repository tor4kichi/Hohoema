using System.Text.Json.Serialization;

namespace NiconicoToolkit.SearchWithCeApi.Video
{
    public sealed class ThreadItem
    {
        [JsonPropertyName("id")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public long Id { get; set; }

        [JsonPropertyName("num_res")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int NumRes { get; set; }

        [JsonPropertyName("summary")]
        public string Summary { get; set; }

        [JsonPropertyName("community_id")]
        public string CommunityId { get; set; }

        [JsonPropertyName("group_type")]
        public string GroupType { get; set; }
    }

    // public enum GroupType { Default };
}

