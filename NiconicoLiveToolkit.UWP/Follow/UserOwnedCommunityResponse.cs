using NiconicoToolkit.Community;
using NiconicoToolkit.User;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using static NiconicoToolkit.Follow.FollowCommunityResponse;
#if WINDOWS_UWP
#else
using System.Net;
using System.Net.Http;
#endif

namespace NiconicoToolkit.Follow
{
    public sealed class UserOwnedCommunityResponse : ResponseWithMeta
    {
        [JsonPropertyName("data")]
        public UserOwnedCommunity Data { get; set; }
    }

    public sealed class UserOwnedCommunity
    {
        [JsonPropertyName("owned")]
        public List<OwnedCommunity> OwnedCommunities { get; set; }
    }

    public sealed class OwnedCommunity : IFollowCommunity
    {
        [JsonPropertyName("id")]
        public CommunityId Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("status")]
        public CommunityStatus Status { get; set; }

        [JsonPropertyName("ownerId")]
        public UserId OwnerId { get; set; }

        [JsonPropertyName("createTime")]
        public DateTimeOffset CreateTime { get; set; }

        [JsonPropertyName("thumbnailUrl")]
        public CommunityThumbnailUrl ThumbnailUrl { get; set; }

        [JsonPropertyName("tags")]
        public List<CommunityTag> Tags { get; set; }

        [JsonPropertyName("userCount")]
        public long UserCount { get; set; }

        [JsonPropertyName("level")]
        public long Level { get; set; }

        [JsonPropertyName("threadMax")]
        public long ThreadMax { get; set; }

        [JsonPropertyName("threadCount")]
        public long ThreadCount { get; set; }

        [JsonPropertyName("optionFlags")]
        public CommunityOptionFlags OptionFlags { get; set; }


        string IFollowCommunity.GlobalId => Id;
    }

}
