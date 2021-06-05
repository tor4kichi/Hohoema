using System.Text.Json.Serialization;

namespace NiconicoToolkit.SearchWithCeApi.Video
{
    public class CeApiResponseBase
    {
        [JsonPropertyName("@status")]
        public string Status { get; set; }

        [JsonIgnore]
        public bool IsOK => Status == "ok";
    }


    public class CeApiResponseContainerBase<T> where T : CeApiResponseBase
    {
        [JsonPropertyName("niconico_response")]
        public T Response { get; set; }
    }


}

