using NiconicoToolkit.Mylist;
using NiconicoToolkit.Video;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NiconicoToolkit.Recommend
{
    public class VideoRecommendResponse : ResponseWithMeta
    {
        [JsonPropertyName("data")]
        public VideoRecommendData Data { get; set; }
    }


    public partial class ReccomendRecipe
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("meta")]
        public object Meta { get; set; }
    }

    public class VideoRecommendData
    {
        [JsonPropertyName("recipe")]
        public ReccomendRecipe Recipe { get; set; }

        [JsonPropertyName("recommendId")]
        public string RecommendId { get; set; }

        [JsonPropertyName("items")]
        [JsonConverter(typeof(RecommendItemContentConverter))]
        public VideoReccomendItem[] Items { get; set; }
    }

    public class VideoReccomendItem
    {
        [JsonPropertyName("id")]
        public NiconicoId Id { get; set; }

        [JsonPropertyName("contentType")]
        public RecommendContentType ContentType { get; set; }

        [JsonPropertyName("recommendType")]
        public RecommendType RecommendType { get; set; }


        public NvapiMylistItem ContentAsMylist { get; set; }

        public NvapiVideoItem ContentAsVideo { get; set; }
    }


}
