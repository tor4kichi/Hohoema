using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NiconicoToolkit.Video.Watch.NMSG_Comment
{
    public class ChatResult
    {
        [JsonPropertyName("thread")]
        public string Thread { get; set; }

        [JsonPropertyName("status")]
        public ChatResultCode Status { get; set; }

        [JsonPropertyName("no")]
        [JsonConverter(typeof(LongToStringConverter))]
        public long? No { get; set; }
    }

    public class PostCommentResponse
    {
        [JsonPropertyName("chat_result")]
        public ChatResult ChatResult { get; set; }
    }
}
