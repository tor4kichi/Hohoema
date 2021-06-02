using NiconicoToolkit.Mylist;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NiconicoToolkit.Follow
{
    public class FollowMylistResponse : ResponseWithMeta
    {
        [JsonPropertyName("data")]
        public FollowMylistData Data { get; set; }
    }

    public class FollowMylistData
    {
        [JsonPropertyName("followLimit")]
        public long FollowLimit { get; set; }

        [JsonPropertyName("mylists")]
        public List<FollowMylist> Mylists { get; set; }
    }

    public class FollowMylist
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("status")]
        public ContentStatus Status { get; set; }

        [JsonPropertyName("detail")]
        public NvapiMylistItem Detail { get; set; }
    }
}
