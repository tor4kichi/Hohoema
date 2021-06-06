using System.Text.Json.Serialization;

namespace NiconicoToolkit
{
    public class CeApiResponseBase
    {
        [JsonPropertyName("@status")]
        public string Status { get; set; }

        [JsonPropertyName("error")]
        public Error Error { get; set; }


        [JsonIgnore]
        public bool IsOK => Status == "ok";
    }


    public sealed class Error
    {
        [JsonPropertyName("code")]
        public string Code { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }
    }

    public class CeApiResponseContainerBase<T> where T : CeApiResponseBase
    {
        [JsonPropertyName("niconico_response")]
        public T Response { get; set; }
    }


}

