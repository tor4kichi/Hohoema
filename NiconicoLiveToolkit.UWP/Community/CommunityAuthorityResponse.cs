using NiconicoToolkit.User;
using System.Text.Json.Serialization;

namespace NiconicoToolkit.Community
{
    public sealed class CommunityAuthorityResponse : ResponseWithMeta
    {
        [JsonPropertyName("data")]
        public CommunityAuthorityData Data { get; set; }


        public sealed class CommunityAuthorityData
        {
            [JsonPropertyName("user_id")]
            public UserId UserId { get; set; }
            [JsonPropertyName("is_owner")]
            public bool IsOwner { get; set; }
            [JsonPropertyName("is_member")]
            public bool IsMember { get; set; }
            [JsonPropertyName("can_post_content")]
            public bool CanPostContent { get; set; }
        }
    }
}
