using NiconicoToolkit.User;
using System;
using System.Text.Json.Serialization;

namespace NiconicoToolkit.Community
{
    public sealed class CommunityInfoResponseContainer : CeApiResponseContainerBase<CommunityInfoResponse>
    {
    }

    public sealed class CommunityInfoResponse : CeApiResponseBase
    {
        [JsonPropertyName("community")]
        public CommunityInfoData Community { get; set; }
    }

    public sealed class CommunityInfoData
    {
        [JsonPropertyName("id")]
        [JsonConverter(typeof(CommunityIdJsonConverter))]
        public CommunityId Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("public")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int Public { get; set; }

        public bool IsPublic => Public != 0;

        [JsonPropertyName("hidden")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int Hidden { get; set; }

        public bool IsHidden => Hidden != 0;

        [JsonPropertyName("user_id")]
        public UserId UserId { get; set; }

        [JsonPropertyName("global_id")]
        public string GlobalId { get; set; }

        [JsonPropertyName("user_count")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int UserCount { get; set; }

        [JsonPropertyName("level")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int Level { get; set; }

        [JsonPropertyName("thumbnail_non_ssl")]
        public Uri ThumbnailNonSsl { get; set; }

        [JsonPropertyName("option_flag")]
        public string OptionFlag { get; set; }

        [JsonPropertyName("option_flag_details")]
        public OptionFlagDetails OptionFlagDetails { get; set; }
    }

    public sealed class OptionFlagDetails
    {
        [JsonPropertyName("community_icon_upload")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int CommunityIconUpload { get; set; }

        public bool IsCommunityIconUpload => CommunityIconUpload != 0;
    }

}
