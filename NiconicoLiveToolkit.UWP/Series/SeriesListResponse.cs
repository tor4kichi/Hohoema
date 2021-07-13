using NiconicoToolkit.Video;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NiconicoToolkit.Series
{
    public sealed class SeriesListResponse : ResponseWithMeta
    {
        [JsonPropertyName("data")]
        public Data Data { get; set; }
    }

    public sealed class Data
    {
        [JsonPropertyName("totalCount")]
        public long TotalCount { get; set; }

        [JsonPropertyName("items")]
        public SeriesItem[] Items { get; set; }
    }

    public sealed class SeriesItem
    {
        [JsonPropertyName("id")]
        public VideoId Id { get; set; }

        [JsonPropertyName("owner")]
        public Owner Owner { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("isListed")]
        public bool IsListed { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("thumbnailUrl")]
        public Uri ThumbnailUrl { get; set; }

        [JsonPropertyName("itemsCount")]
        public long ItemsCount { get; set; }
    }

    public sealed class Owner
    {
        [JsonPropertyName("type")]
        public OwnerType Type { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; }
    }
}
