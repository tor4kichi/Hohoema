using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NiconicoToolkit.Video.Watch.Dmc
{
    public class ContentAuth_Response : ContentAuth_Request
    {
        // res
        [JsonPropertyName("max_content_count")]
        public int MaxContentCount { get; set; }

        // res
        [JsonPropertyName("content_auth_info")]
        public ContentAuthInfo ContentAuthInfo { get; set; }
    }

    public class ContentAuth_Request
    {
        // res req
        [JsonPropertyName("auth_type")]
        public string AuthType { get; set; }

        // res req
        [JsonPropertyName("content_key_timeout")]
        public int ContentKeyTimeout { get; set; }

        // res req
        [JsonPropertyName("service_id")]
        public string ServiceId { get; set; }

        // res req
        [JsonPropertyName("service_user_id")]
        public string ServiceUserId { get; set; }
    }

    public class ContentAuthInfo
    {

        [JsonPropertyName("method")]
        public string Method { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("value")]
        public string Value { get; set; }
    }


}
