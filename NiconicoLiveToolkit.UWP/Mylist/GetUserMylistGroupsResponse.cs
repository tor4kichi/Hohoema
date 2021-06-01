using System.Text.Json.Serialization;

namespace NiconicoToolkit.Mylist
{
    public sealed class GetUserMylistGroupsResponse : ResponseWithMeta
    {
        [JsonPropertyName("data")]
        public GetUserMylistGroupsData Data { get; set; }
    }

    public sealed class GetUserMylistGroupsData
    {
        [JsonPropertyName("mylists")]
        public NvapiMylistItem[] MylistGroups { get; set; }
    }


}
