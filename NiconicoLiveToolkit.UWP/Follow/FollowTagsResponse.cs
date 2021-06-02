using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NiconicoToolkit.Follow
{
    public partial class FollowTagsResponse : ResponseWithMeta
    {
        [JsonPropertyName("data")]
        public FollowTagsData Data { get; set; }

        public partial class FollowTagsData
        {
            [JsonPropertyName("tags")]
            public List<Tag> Tags { get; set; }
        }

        public partial class Tag
        {
            [JsonPropertyName("name")]
            public string Name { get; set; }

            [JsonPropertyName("followedAt")]
            public DateTimeOffset FollowedAt { get; set; }

            [JsonPropertyName("nicodicSummary")]
            public string NicodicSummary { get; set; }
        }
    }
}
