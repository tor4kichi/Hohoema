using NiconicoToolkit.Community;
using NiconicoToolkit.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NiconicoToolkit.Follow
{
    public sealed class FollowCommunityResponse : ResponseWithMeta<FollowCommunityMeta>
    {
        [JsonPropertyName("data")]
        public FollowCommunity[] Data { get; set; }
    }

    public sealed class FollowCommunity : IFollowCommunity
    {
        [JsonPropertyName("id")]
        public CommunityId Id { get; set; }

        [JsonPropertyName("globalId")]
        public string GlobalId { get; set; }

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
    }



    public sealed class CommunityOptionFlags
    {
        [JsonPropertyName("communityAutoAcceptEntry")]
        public bool CommunityAutoAcceptEntry { get; set; }

        [JsonPropertyName("communityBlomaga")]
        public bool CommunityBlomaga { get; set; }

        [JsonPropertyName("communityHideLiveArchives")]
        public bool CommunityHideLiveArchives { get; set; }

        [JsonPropertyName("communityInvalidBbs")]
        public bool CommunityInvalidBbs { get; set; }

        [JsonPropertyName("communityPrivLiveBroadcastNew")]
        public bool CommunityPrivLiveBroadcastNew { get; set; }

        [JsonPropertyName("communityPrivUserAuth")]
        public bool CommunityPrivUserAuth { get; set; }

        [JsonPropertyName("communityPrivVideoPost")]
        public bool CommunityPrivVideoPost { get; set; }

        [JsonPropertyName("communityShownNewsNum")]
        public long CommunityShownNewsNum { get; set; }

        [JsonPropertyName("communityUserInfoRequired")]
        public bool CommunityUserInfoRequired { get; set; }

        [JsonPropertyName("communityIconInspectionMobile")]
        public string CommunityIconInspectionMobile { get; set; }
    }

    public sealed class CommunityTag
    {
        [JsonPropertyName("text")]
        public string Text { get; set; }
    }

    public sealed class CommunityThumbnailUrl
    {
        [JsonPropertyName("normal")]
        public Uri Normal { get; set; }

        [JsonPropertyName("small")]
        public Uri Small { get; set; }
    }

    public class FollowCommunityMeta : Meta
    {
        [JsonPropertyName("total")]
        public long Total { get; set; }

        [JsonPropertyName("count")]
        public long Count { get; set; }
    }


    public enum CommunityStatus { Closed, Open };
}
