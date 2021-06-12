using System;
using System.Text.Json.Serialization;

namespace NiconicoToolkit.User
{
    public sealed class NicovideoUserResponseContainer : CeApiResponseContainerBase<NicovideoUserResponse>
    {
    }


    public sealed class NicovideoUserResponse : CeApiResponseBase
    {
        [JsonPropertyName("user")]
        public UserInfo User { get; set; }

        [JsonPropertyName("vita_option")]
        public VitaOption VitaOption { get; set; }

        [JsonPropertyName("additionals")]
        public string Additionals { get; set; }
    }

    public partial class UserInfo
    {
        [JsonPropertyName("id")]
        public UserId Id { get; set; }

        [JsonPropertyName("nickname")]
        public string Nickname { get; set; }

        [JsonPropertyName("thumbnail_url")]
        public Uri ThumbnailUrl { get; set; }
    }

    public partial class VitaOption
    {
        [JsonPropertyName("user_secret")]
        public string UserSecret { get; set; }
    }

}
