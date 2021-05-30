using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NiconicoToolkit.Video.Watch.Dmc
{
    public class ContentSrcIdSet
    {
        // res req
        [JsonPropertyName("content_src_ids")]
        public IList<ContentSrcId> ContentSrcIds { get; set; }

        // res
        [JsonPropertyName("allow_subset")]
        public string AllowSubset { get; set; }
    }

    public class ContentSrcId
    {

        [JsonPropertyName("src_id_to_mux")]
        public SrcIdToMux SrcIdToMux { get; set; }
    }

    public class SrcIdToMux
    {

        [JsonPropertyName("video_src_ids")]
        public IList<string> VideoSrcIds { get; set; }

        [JsonPropertyName("audio_src_ids")]
        public IList<string> AudioSrcIds { get; set; }
    }
}
