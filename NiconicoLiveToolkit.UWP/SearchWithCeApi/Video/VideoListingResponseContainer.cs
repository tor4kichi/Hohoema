using System;
using System.Text.Json.Serialization;

namespace NiconicoToolkit.SearchWithCeApi.Video
{
    public sealed class VideoListingResponseContainer : CeApiResponseContainerBase<VideoListingResponse>
    {
    }


    public sealed class VideoListingResponse : CeApiResponseBase
    {
        [JsonPropertyName("total_count")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public long TotalCount { get; set; }

        [JsonPropertyName("video_info")]
        public VideoInfo[] Videos { get; set; }

        [JsonPropertyName("tags")]
        public Tags Tags { get; set; }
    }

    public sealed class Tags
    {
        [JsonPropertyName("tag")]
        public Tag[] Tag { get; set; }
    }

    public sealed class Tag
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
    }

    public sealed class VideoInfo
    {
        [JsonPropertyName("video")]
        public VideoItem Video { get; set; }

        [JsonPropertyName("thread")]
        public ThreadItem Thread { get; set; }
    }

    
}
