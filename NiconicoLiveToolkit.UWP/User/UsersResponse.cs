using System;
using System.Text.Json.Serialization;

namespace NiconicoToolkit.User
{
    public sealed class UsersResponse : ResponseWithMeta
    {
        [JsonPropertyName("data")]
        public UsersData[] Data { get; set; }


        public sealed class UsersData
        {
            [JsonPropertyName("userId")]
            public UserId UserId { get; set; }

            [JsonPropertyName("nickname")]
            public string Nickname { get; set; }

            [JsonPropertyName("description")]
            public string Description { get; set; }

            [JsonPropertyName("hasPremiumOrStrongerRights")]
            public bool HasPremiumOrStrongerRights { get; set; }

            [JsonPropertyName("hasSuperPremiumOrStrongerRights")]
            public bool HasSuperPremiumOrStrongerRights { get; set; }

            [JsonPropertyName("icons")]
            public Icons Icons { get; set; }
        }

        public sealed class Icons
        {
            [JsonPropertyName("urls")]
            public Urls Urls { get; set; }
        }

        public sealed class Urls
        {
            [JsonPropertyName("150x150")]
            public Uri The150X150 { get; set; }

            [JsonPropertyName("50x50")]
            public Uri The50X50 { get; set; }
        }
    }

}
