using NiconicoToolkit.Video;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NiconicoToolkit.User
{
    public sealed class UserVideoResponse : ResponseWithMeta
    {
        [JsonPropertyName("data")]
        public UserVideoData Data { get; set; }
    }

    public sealed class UserVideoData
    {
        [JsonPropertyName("totalCount")]
        public long TotalCount { get; set; }

        [JsonPropertyName("items")]
        public UserVideoItem[] Items { get; set; }
    }

    public sealed class UserVideoItem
    {
        [JsonPropertyName("series")]
        public Series Series { get; set; }

        [JsonPropertyName("essential")]
        public NvapiVideoItem Essential { get; set; }
    }

    public sealed class Series
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("order")]
        public long Order { get; set; }
    }
}
