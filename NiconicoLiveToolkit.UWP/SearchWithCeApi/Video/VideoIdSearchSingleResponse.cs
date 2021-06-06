using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NiconicoToolkit.SearchWithCeApi.Video
{
    public sealed class VideoSingleIdSearchResponseContainer : CeApiResponseContainerBase<VideoIdSearchSingleResponse>
    {
    }

    public sealed class VideoIdSearchSingleResponse : CeApiResponseBase
    {
        [JsonPropertyName("video")]
        public VideoItem Video { get; set; }

        [JsonPropertyName("thread")]
        public ThreadItem Thread { get; set; }

        [JsonPropertyName("tags")]
        public VideoTags Tags { get; set; }
    }

    public sealed class VideoTags
    {
        [JsonPropertyName("tag_info")]
        [JsonConverter(typeof(SingleOrArrayConverter<List<TagInfo>, TagInfo>))]
        public List<TagInfo> TagInfo { get; set; }
    }

    public sealed class TagInfo
    {
        [JsonPropertyName("tag")]
        public string Tag { get; set; }

        [JsonPropertyName("area")]
        public string Area { get; set; }
    }    
}

