using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
#if WINDOWS_UWP
using Windows.Web.Http;
using Windows.Web.Http.Headers;
#else
using System.Net.Http;
using System.Net.Http.Headers;
#endif

namespace NiconicoToolkit
{
    public class ResponseWithMeta
    {
        [JsonPropertyName("meta")]
        public Meta Meta { get; set; }

        public bool IsSuccess => Meta.IsSuccess;


        public static T CreateFromStatusCode<T>(HttpStatusCode code)
            where T : ResponseWithMeta, new()
        {
            return new T() { Meta = new Meta() { Status = (long)code } };
        }
    }

    public class ResponseWithMeta<MetaClassType> where MetaClassType : Meta
    {
        [JsonPropertyName("meta")]
        public MetaClassType Meta { get; set; }

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
