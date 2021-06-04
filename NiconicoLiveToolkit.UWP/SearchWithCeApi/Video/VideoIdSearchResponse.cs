using System.Text.Json.Serialization;

namespace NiconicoToolkit.SearchWithCeApi.Video
{
    public sealed class VideoIdSearchResponseContainer : CeApiResponseContainerBase<VideoIdSearchResponse>
    {
    }


    public class VideoIdSearchResponse : CeApiResponseBase
    {
        [JsonPropertyName("video_info")]
        public VideoInfo[] Videos { get; set; }
    }
}

