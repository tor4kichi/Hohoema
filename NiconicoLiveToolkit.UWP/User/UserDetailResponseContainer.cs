using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NiconicoToolkit.User
{
    public sealed class UserDetailResponseContainer
    {
        [JsonPropertyName("userDetails")]
        public UserDetailResponse Detail { get; set; }
    }


    public sealed class UserDetailResponse : ResponseWithMeta
    {
        [JsonPropertyName("userDetails")]
        public UserDetailData Data { get; set; }
    }

    public sealed class UserDetailData
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("user")]
        public UserDetail User { get; set; }

        [JsonPropertyName("followStatus")]
        public FollowStatus FollowStatus { get; set; }
    }

    public sealed class FollowStatus
    {
        [JsonPropertyName("isFollowing")]
        public bool IsFollowing { get; set; }
    }

    public sealed class UserDetail
    {
        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("strippedDescription")]
        public string StrippedDescription { get; set; }

        [JsonPropertyName("isPremium")]
        public bool IsPremium { get; set; }

        [JsonPropertyName("registeredVersion")]
        public string RegisteredVersion { get; set; }

        [JsonPropertyName("followeeCount")]
        public long FolloweeCount { get; set; }

        [JsonPropertyName("followerCount")]
        public long FollowerCount { get; set; }

        [JsonPropertyName("userLevel")]
        public UserLevel UserLevel { get; set; }

        [JsonPropertyName("userChannel")]
        public UserChannel UserChannel { get; set; }

        [JsonPropertyName("isNicorepoReadable")]
        public bool IsNicorepoReadable { get; set; }

        //[JsonPropertyName("sns")]
        //public List<string> Sns { get; set; }

        [JsonPropertyName("id")]
        public UserId Id { get; set; }

        [JsonPropertyName("nickname")]
        public string Nickname { get; set; }

        [JsonPropertyName("icons")]
        public Icons Icons { get; set; }
    }

    public sealed class Icons
    {
        [JsonPropertyName("small")]
        public Uri Small { get; set; }

        [JsonPropertyName("large")]
        public Uri Large { get; set; }
    }

    public sealed class UserLevel
    {
        [JsonPropertyName("currentLevel")]
        public long CurrentLevel { get; set; }

        [JsonPropertyName("nextLevelThresholdExperience")]
        public long NextLevelThresholdExperience { get; set; }

        [JsonPropertyName("nextLevelExperience")]
        public long NextLevelExperience { get; set; }

        [JsonPropertyName("currentLevelExperience")]
        public long CurrentLevelExperience { get; set; }
    }

    public partial class UserChannel
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("thumbnailUrl")]
        public Uri ThumbnailUrl { get; set; }

        [JsonPropertyName("thumbnailSmallUrl")]
        public Uri ThumbnailSmallUrl { get; set; }
    }
}
