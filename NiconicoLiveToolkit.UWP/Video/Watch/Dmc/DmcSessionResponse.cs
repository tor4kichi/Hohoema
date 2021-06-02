using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NiconicoToolkit.Video.Watch.Dmc
{
    public class DmcSessionResponse
    {
        [JsonPropertyName("meta")]
        public DmcSessionResponseMeta Meta { get; set; }

        [JsonPropertyName("data")]
        public Data Data { get; set; }
    }

    public class DmcSessionResponseMeta : Meta
    {
        [JsonPropertyName("message")]
        public string Message { get; set; }
    }

    public class Data
    {

        [JsonPropertyName("session")]
        public ResponseSession Session { get; set; }
    }

    public class ResponseSession
    {
        // res req
        [JsonPropertyName("recipe_id")]
        public string RecipeId { get; set; }

        // res req
        [JsonPropertyName("content_id")]
        public string ContentId { get; set; }

        // res req
        [JsonPropertyName("content_src_id_sets")]
        public IList<ContentSrcIdSet> ContentSrcIdSets { get; set; }

        // res req
        [JsonPropertyName("content_type")]
        public string ContentType { get; set; }

        // res req
        [JsonPropertyName("timing_constraint")]
        public string TimingConstraint { get; set; }

        // res req
        [JsonPropertyName("keep_method")]
        public KeepMethod KeepMethod { get; set; }

        // res req
        [JsonPropertyName("protocol")]
        public Protocol Protocol { get; set; }

        // res req (req is string.Empty)
        [JsonPropertyName("content_uri")]
        public Uri ContentUri { get; set; }

        // res req
        [JsonPropertyName("session_operation_auth")]
        public SessionOperationAuth_Response SessionOperationAuth { get; set; }

        // res req
        [JsonPropertyName("content_auth")]
        public ContentAuth_Response ContentAuth { get; set; }

        // res req
        [JsonPropertyName("client_info")]
        public ClientInfo ClientInfo { get; set; }

        // res req
        [JsonPropertyName("priority")]
        public double Priority { get; set; }

        // res
        [JsonPropertyName("id")]
        public string Id { get; set; }

        // res
        [JsonPropertyName("play_seek_time")]
        public int PlaySeekTime { get; set; }

        // res
        [JsonPropertyName("play_speed")]
        public double PlaySpeed { get; set; }

        // res
        [JsonPropertyName("runtime_info")]
        public RuntimeInfo RuntimeInfo { get; set; }

        // res
        [JsonPropertyName("created_time")]
        public long CreatedTime { get; set; }

        // res
        [JsonPropertyName("modified_time")]
        public long ModifiedTime { get; set; }

        // res
        [JsonPropertyName("content_route")]
        public int ContentRoute { get; set; }

        // res
        [JsonPropertyName("version")]
        public string Version { get; set; }

        // res
        [JsonPropertyName("content_status")]
        public string ContentStatus { get; set; }
    }

    public class RuntimeInfo
    {

        [JsonPropertyName("node_id")]
        public string NodeId { get; set; }

        [JsonPropertyName("execution_history")]
        public IList<object> ExecutionHistory { get; set; }
    }


}
