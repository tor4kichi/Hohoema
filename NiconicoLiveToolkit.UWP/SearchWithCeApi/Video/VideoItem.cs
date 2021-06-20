using NiconicoToolkit.User;
using NiconicoToolkit.Video;
using System;
using System.Text.Json.Serialization;

namespace NiconicoToolkit.SearchWithCeApi.Video
{
    public class VideoItem
    {
        [JsonPropertyName("id")]
        public VideoId Id { get; set; }

        [JsonPropertyName("user_id")]
        public UserId UserId { get; set; }

        [JsonPropertyName("deleted")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int Deleted { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("length_in_seconds")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int LengthInSeconds { get; set; }

        [JsonPropertyName("thumbnail_url")]
        public Uri ThumbnailUrl { get; set; }

        [JsonPropertyName("first_retrieve")]
        public DateTimeOffset FirstRetrieve { get; set; }

        [JsonPropertyName("view_counter")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int ViewCount { get; set; }

        [JsonPropertyName("mylist_counter")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int MylistCount { get; set; }

        [JsonPropertyName("community_id")]
        public NiconicoId CommunityId { get; set; }

        [JsonPropertyName("ppv_video")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int PpvVideo { get; set; }

        [JsonPropertyName("provider_type")]
        public VideoProviderType ProviderType { get; set; }



        [JsonIgnore]
        public TimeSpan Duration => TimeSpan.FromSeconds(LengthInSeconds);

        [JsonIgnore]
        public bool IsPayRequired => PpvVideo == 1;

        [JsonIgnore]
        public bool IsDeleted => Deleted != 0;

        /*
        [JsonIgnore]
        public VideoPermission VideoPermission
        {
            get => Permission switch
            {
                "" or "0" => VideoPermission.None,
                "1" => IsPayRequired ? VideoPermission.RequirePay : VideoPermission.RequirePremiumMember,
                "2" => VideoPermission.FreeForChannelMember,
                "3" => VideoPermission.VideoPermission_3,
                "4" => VideoPermission.MemberUnlimitedAccess,
                _ => VideoPermission.Unknown,
            };
        }
        */
    }
}

