using System.Text.Json.Serialization;
#if WINDOWS_UWP
#else
using System.Net;
using System.Net.Http;
#endif

namespace NiconicoToolkit.Follow
{
    public class FollowedResultResponce : ResponseWithMeta
    {
        [JsonPropertyName("data")]
        public FollowedData Data { get; set; }
    }


    public class FollowedData
    {
        [JsonPropertyName("following")]
        public bool IsFollowing { get; set; }
    }


}
