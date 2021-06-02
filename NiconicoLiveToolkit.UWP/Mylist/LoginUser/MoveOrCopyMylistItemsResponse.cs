using System.Text.Json.Serialization;

namespace NiconicoToolkit.Mylist.LoginUser
{
    public sealed class MoveOrCopyMylistItemsResponse : ResponseWithMeta
    {
        [JsonPropertyName("data")]
        public MoveOrCopyMylistItemsData Data { get; set; }
    }



    public sealed class MoveOrCopyMylistItemsData
    {
        [JsonPropertyName("duplicatedIds")]
        public string[] DuplicatedIds { get; set; }

        [JsonPropertyName("processedIds")]
        public string[] ProcessedIds { get; set; }

    }

}
