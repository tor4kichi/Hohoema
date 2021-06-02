using System.Text.Json.Serialization;
#if WINDOWS_UWP
#else
using System.Net;
using System.Net.Http;
#endif

namespace NiconicoToolkit.Follow
{
    public class CommunityAuthorityResponse
    {
        [JsonPropertyName("meta")]
        public Meta Meta { get; set; }

        [JsonPropertyName("data")]
        public CommunityAuthority Data { get; set; }
    }


    public class CommunityAuthority
    {
        [JsonPropertyName("user_id")]
        public long UserId { get; set; }

        [JsonPropertyName("is_owner")]
        public bool IsOwner { get; set; }

        [JsonPropertyName("is_member")]
        public bool IsMember { get; set; }

        [JsonPropertyName("can_post_content")]
        public bool CanPostContent { get; set; }
    }

}
