using NiconicoToolkit.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NiconicoToolkit.Follow
{
    public partial class FollowUsersResponse : ResponseWithMeta
    {
        [JsonPropertyName("data")]
        public FollowUserData Data { get; set; }
    }

    public partial class FollowUserData
    {
        [JsonPropertyName("items")]
        public List<UserFollowItem> Items { get; set; }

        [JsonPropertyName("summary")]
        public Summary Summary { get; set; }
    }

    public partial class UserFollowItem
    {
        [JsonPropertyName("type")]
        //public FollowType Type { get; set; }
        public string Type { get; set; }

        [JsonPropertyName("relationships")]
        public Relationships Relationships { get; set; }

        [JsonPropertyName("isPremium")]
        public bool IsPremium { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("strippedDescription")]
        public string StrippedDescription { get; set; }

        [JsonPropertyName("id")]
        public UserId Id { get; set; }

        [JsonPropertyName("nickname")]
        public string Nickname { get; set; }

        [JsonPropertyName("icons")]
        public Icons Icons { get; set; }
    }

    public partial class Icons
    {
        [JsonPropertyName("small")]
        public Uri Small { get; set; }

        [JsonPropertyName("large")]
        public Uri Large { get; set; }
    }

    public partial class Relationships
    {
        [JsonPropertyName("sessionUser")]
        public SessionUser SessionUser { get; set; }
    }

    public partial class SessionUser
    {
        [JsonPropertyName("isFollowing")]
        public bool IsFollowing { get; set; }
    }

    public partial class Summary
    {
        [JsonPropertyName("followees")]
        public long Followees { get; set; }

        [JsonPropertyName("followers")]
        public long Followers { get; set; }

        [JsonPropertyName("hasNext")]
        public bool HasNext { get; set; }

        [JsonPropertyName("cursor")]
        public string Cursor { get; set; }
    }    

    public enum FollowType { Relationship };

}
