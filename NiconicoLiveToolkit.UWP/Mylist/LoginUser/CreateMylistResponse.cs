using System.Text.Json.Serialization;

namespace NiconicoToolkit.Mylist.LoginUser
{
    public sealed class CreateMylistResponse : ResponseWithMeta
    {
        [JsonPropertyName("data")]
        public CreateMylistData Data { get; set; }
    }


    public sealed class CreateMylistData
    {
        [JsonPropertyName("mylistId")]
        public long MylistId { get; set; }
    }

}
