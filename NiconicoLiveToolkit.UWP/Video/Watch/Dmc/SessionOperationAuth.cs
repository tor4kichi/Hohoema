using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NiconicoToolkit.Video.Watch.Dmc
{
    public class SessionOperationAuth_Response
    {

        [JsonPropertyName("session_operation_auth_by_signature")]
        public SessionOperationAuthBySignature_Response SessionOperationAuthBySignature { get; set; }
    }

    public class SessionOperationAuthBySignature_Response : SessionOperationAuthBySignature_Request
    {
        // Res
        [JsonPropertyName("created_time")]
        public long CreatedTime { get; set; }

        // Res
        [JsonPropertyName("expire_time")]
        public long ExpireTime { get; set; }

    }


    public class SessionOperationAuth_Request
    {

        [JsonPropertyName("session_operation_auth_by_signature")]
        public SessionOperationAuthBySignature_Request SessionOperationAuthBySignature { get; set; }
    }

    public class SessionOperationAuthBySignature_Request
    {

        // Res/Req
        [JsonPropertyName("token")]
        public string Token { get; set; }

        // Res/Req
        [JsonPropertyName("signature")]
        public string Signature { get; set; }
    }

}
