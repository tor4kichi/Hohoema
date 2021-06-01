using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NiconicoToolkit.Video.Watch.Dmc
{
    public class DmcSessionRequest
    {
        [JsonPropertyName("session")]
        public RequestSession Session { get; set; }
    }

    public class RequestSession
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
        public string ContentUri { get; set; }

        // res req
        [JsonPropertyName("session_operation_auth")]
        public SessionOperationAuth_Request SessionOperationAuth { get; set; }

        // res req
        [JsonPropertyName("content_auth")]
        public ContentAuth_Request ContentAuth { get; set; }

        // res req
        [JsonPropertyName("client_info")]
        public ClientInfo ClientInfo { get; set; }

        // res req
        [JsonPropertyName("priority")]
        public double Priority { get; set; }

    }



}
