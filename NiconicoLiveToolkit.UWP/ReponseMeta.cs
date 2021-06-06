using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NiconicoToolkit
{
    public class ResponseWithMeta
    {
        [JsonPropertyName("meta")]
        public Meta Meta { get; set; }

        public bool IsSuccess => Meta.IsSuccess;
    }

    public class Meta
    {
        [JsonPropertyName("status")]
        public long Status { get; set; }

        [JsonPropertyName("errorCode")]
        public string ErrorCode { get; set; }
        

        public bool IsSuccess => HttpStatusCodeHelper.IsSuccessStatusCode(Status);
    }
}
