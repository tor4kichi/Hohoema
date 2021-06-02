using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NiconicoToolkit.Mylist.LoginUser
{
    public sealed class LoginUserMylistsResponse : ResponseWithMeta
    {
        [JsonPropertyName("data")]
        public MylistGroups Data { get; set; }
    }

    public sealed class MylistGroups
    {
        [JsonPropertyName("mylists")]
        public NvapiMylistItem[] Mylists { get; set; }
    }
}
